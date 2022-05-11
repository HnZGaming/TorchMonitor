using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class OnlinePlayersMonitor : IIntervalListener
    {
        const int IntervalSecs = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly NameConflictSolver<ulong> _nameConflictSolver;
        readonly PlayerOnlineTimeDb _playerOnlineTimeDb;
        readonly TorchMonitorNexus _nexus;

        public OnlinePlayersMonitor(
            NameConflictSolver<ulong> nameConflictSolver,
            PlayerOnlineTimeDb playerOnlineTimeDb,
            TorchMonitorNexus nexus)
        {
            _nameConflictSolver = nameConflictSolver;
            _playerOnlineTimeDb = playerOnlineTimeDb;
            _nexus = nexus;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % IntervalSecs != 0) return;

            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();
            var factionList = MySession.Static.Factions.Factions.Values;
            var factions = new Dictionary<string, int>();

            Log.Trace("writing players_players");

            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) continue;

                var playerId = onlinePlayer.PlayerId();
                var steamId = onlinePlayer.SteamId();
                if (steamId == 0) continue;

                _playerOnlineTimeDb.IncrementPlayerOnlineTime(steamId, (double)IntervalSecs / 3600);
                var onlineTime = _playerOnlineTimeDb.GetPlayerOnlineTime(steamId);

                var faction = factionList.FirstOrDefault(f => f.Members.ContainsKey(playerId));
                var factionTag = faction?.Tag ?? "<single>";
                factions.Increment(factionTag);

                // animal
                if (string.IsNullOrEmpty(onlinePlayer.DisplayName) &&
                    TorchMonitorConfig.Instance.IgnoreAnimals)
                {
                    continue;
                }

                var playerName = onlinePlayer.DisplayName;
                playerName = _nameConflictSolver.GetSafeName(playerName, steamId);

                Log.Trace($"player name: {playerName}, faction tag: {factionTag}");

                TorchInfluxDbWriter
                    .Measurement("players_players")
                    .Tag("player_name", playerName)
                    .Tag("faction_tag", factionTag)
                    .Tag("steam_id", $"{steamId}")
                    .Field("is_online", 1)
                    .Field("online_time", onlineTime)
                    .Write();
            }

            Log.Trace("writing players_factions");

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                Log.Trace($"faction tag: {factionTag}");

                TorchInfluxDbWriter
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
            }

            Log.Trace("writing server");

            var totalOnlineTime = _playerOnlineTimeDb.GetTotalOnlineTime();

            TorchInfluxDbWriter
                .Measurement("server")
                .Field("players", onlinePlayers.Length)
                .Field("online_time", totalOnlineTime)
                .Write();

            // nexus
            if (TorchMonitorConfig.Instance.EnableNexusFeature)
            {
                Log.Trace("writing nexus");

                var segments = _nexus.GetSegmentedPopulation(onlinePlayers);
                foreach (var (segmentName, playerCount) in segments)
                {
                    // save space
                    if (playerCount == 0) continue;

                    TorchInfluxDbWriter
                        .Measurement("nexus")
                        .Tag("segment", segmentName)
                        .Field("players", playerCount)
                        .Write();

                    Log.Debug($"nexus segmented pop: {segmentName} -> {playerCount}");
                }
            }

            _playerOnlineTimeDb.WriteToDb();
        }
    }
}