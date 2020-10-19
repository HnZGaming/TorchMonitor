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

            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                var playerName = player.DisplayName;
                var factionOrNull = GetFactionOrNull(player);
                var factionTag = factionOrNull?.Tag ?? "<single>";

                var point = _dbClient
                    .MakePointIn("players_players")
                    .Tag("player_name", playerName)
                    .Tag("faction_tag", factionTag)
                    .Field("logged_in", 1);

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

        IMyFaction GetFactionOrNull(MyPlayer player)
        {
            var playerId = player.Identity.IdentityId;
            var factionOrNull = _factions.FirstOrDefault(f => f.Members.ContainsKey(playerId));
            return factionOrNull;
        }
    }
}