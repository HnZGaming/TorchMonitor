using System.Collections.Generic;
using System.Linq;
using InfluxDb;
using Intervals;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class OnlinePlayersMonitor : IIntervalListener
    {
        const int IntervalSecs = 10;
        readonly NameConflictSolver _nameConflictSolver;
        readonly PlayerOnlineTimeDb _playerOnlineTimeDb;

        public OnlinePlayersMonitor(
            NameConflictSolver nameConflictSolver,
            PlayerOnlineTimeDb playerOnlineTimeDb)
        {
            _nameConflictSolver = nameConflictSolver;
            _playerOnlineTimeDb = playerOnlineTimeDb;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % IntervalSecs != 0) return;

            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();

            var factionList = MySession.Static.Factions.Factions.Values;
            var factions = new Dictionary<string, int>();

            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) continue;

                var steamId = onlinePlayer.SteamId();
                var playerId = onlinePlayer.PlayerId();

                _playerOnlineTimeDb.IncrementOnlineTime(steamId, (double) IntervalSecs / 3600);
                var onlineTime = _playerOnlineTimeDb.GetOnlineTime(steamId);

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

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                InfluxDbPointFactory
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
            }

            var totalOnlineTime = _playerOnlineTimeDb.GetTotalOnlineTime();

            InfluxDbPointFactory
                .Measurement("server")
                .Field("players", onlinePlayers.Length)
                .Field("online_time", totalOnlineTime)
                .Write();

            _playerOnlineTimeDb.Apply();
        }
    }
}