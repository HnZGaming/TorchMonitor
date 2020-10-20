using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDB.Client.Writes;
using Sandbox.Game.World;
using Torch.Server.InfluxDb;
using VRage.Game.ModAPI;

namespace TorchMonitor.Business
{
    public sealed class PlayersMonitor : IMonitor
    {
        readonly InfluxDbClient _dbClient;
        readonly List<IMyFaction> _factions;

        public PlayersMonitor(InfluxDbClient dbClient)
        {
            _dbClient = dbClient;
            _factions = new List<IMyFaction>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            _factions.Clear();
            _factions.AddRange(MySession.Static.Factions.Factions.Values);

            var points = new List<PointData>();
            var onlineMemberCountPerFactions = new Dictionary<string, int>();

            var allIdentities = MySession.Static.Players.GetAllIdentities();
            foreach (var player in allIdentities)
            {
                if (!GetActiveTimeIfOnline(player, out var activeTime)) continue;

                var playerName = player.DisplayName;
                var factionTag = GetFactionOrNull(player)?.Tag ?? "<single>";

                var point = _dbClient
                    .MakePointIn("players_players")
                    .Tag("player_name", playerName)
                    .Tag("faction_tag", factionTag)
                    .Field("active_time", activeTime.TotalMinutes);

                points.Add(point);

                onlineMemberCountPerFactions.TryGetValue(factionTag, out var factionMemberCount);
                onlineMemberCountPerFactions[factionTag] = factionMemberCount + 1;
            }

            foreach (var (factionTag, onlineMemberCount) in onlineMemberCountPerFactions)
            {
                var point = _dbClient
                    .MakePointIn("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount);

                points.Add(point);
            }

            _dbClient.WritePoints(points.ToArray());
        }

        bool GetActiveTimeIfOnline(MyIdentity player, out TimeSpan activeTime)
        {
            activeTime = default;

            if (!MySession.Static.Players.IsPlayerOnline(player.IdentityId)) return false;

            activeTime = DateTime.Now - player.LastLoginTime;
            return true;
        }

        IMyFaction GetFactionOrNull(MyIdentity player)
        {
            var playerId = player.IdentityId;
            var factionOrNull = _factions.FirstOrDefault(f => f.Members.ContainsKey(playerId));
            return factionOrNull;
        }
    }
}