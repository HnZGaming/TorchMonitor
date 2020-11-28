using System.Collections.Generic;
using System.Linq;
using InfluxDb;
using Intervals;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed partial class OnlinePlayersMonitor : IIntervalListener
    {
        const int IntervalSecs = 10;
        readonly NameConflictSolver _nameConflictSolver;
        readonly StupidJsonDb _localDb;

        public OnlinePlayersMonitor(
            NameConflictSolver nameConflictSolver,
            StupidJsonDb localDb)
        {
            _nameConflictSolver = nameConflictSolver;
            _localDb = localDb;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % IntervalSecs != 0) return;

            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();
            var steamIds = onlinePlayers.Select(p => p.SteamId());
            var onlineTimes = GetOnlineTimeFromDb(steamIds);

            var factionList = MySession.Static.Factions.Factions.Values;
            var factions = new Dictionary<string, int>();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) continue;

                var steamId = onlinePlayer.SteamId();
                var playerId = onlinePlayer.PlayerId();

                onlineTimes.TryGetValue(steamId, out var onlineTime);
                onlineTime += (double) IntervalSecs / 60;
                onlineTimes[steamId] = onlineTime;

                var faction = factionList.FirstOrDefault(f => f.Members.ContainsKey(playerId));
                var factionTag = faction?.Tag ?? "<single>";
                factions.Increment(factionTag);

                var playerName = onlinePlayer.DisplayName;
                playerName = _nameConflictSolver.GetSafeName(playerName, onlinePlayer.PlayerId());

                InfluxDbPointFactory
                    .Measurement("players_players")
                    .Tag("steam_id", $"{steamId}")
                    .Tag("player_name", playerName)
                    .Tag("faction_tag", factionTag)
                    .Field("is_online", 1)
                    .Field("online_time", onlineTime)
                    .Write();
            }

            SetOnlineTimeToDb(onlineTimes);

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                InfluxDbPointFactory
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
            }

            var totalOnlineTime = onlineTimes.Select(t => t.Value).Sum();

            InfluxDbPointFactory
                .Measurement("server")
                .Field("players", onlinePlayers.Length)
                .Field("online_time", totalOnlineTime)
                .Write();
        }

        IDictionary<ulong, double> GetOnlineTimeFromDb(IEnumerable<ulong> steamIds)
        {
            var dic = new Dictionary<ulong, double>();

            var steamIdSet = new HashSet<ulong>(steamIds);
            var rows = _localDb.Query<PlayerOnlineTime>("online_times");
            foreach (var row in rows)
            {
                var steamId = ulong.Parse(row.SteamId);
                if (steamIdSet.Contains(steamId))
                {
                    dic[steamId] = row.OnlineTime;
                }
            }

            return dic;
        }

        void SetOnlineTimeToDb(IDictionary<ulong, double> onlineTimes)
        {
            var values = new List<PlayerOnlineTime>();
            foreach (var (steamId, onlineTime) in onlineTimes)
            {
                values.Add(new PlayerOnlineTime
                {
                    SteamId = $"{steamId}",
                    OnlineTime = onlineTime,
                });
            }

            _localDb.Insert("online_times", values);
            _localDb.Write();
        }
    }
}