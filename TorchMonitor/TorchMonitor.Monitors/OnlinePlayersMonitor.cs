﻿using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class OnlinePlayersMonitor : IIntervalListener
    {
        const int IntervalSecs = 10;
        readonly NameConflictSolver<ulong> _nameConflictSolver;
        readonly PlayerOnlineTimeDb _playerOnlineTimeDb;

        public OnlinePlayersMonitor(
            NameConflictSolver<ulong> nameConflictSolver,
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

                var playerId = onlinePlayer.PlayerId();
                var steamId = onlinePlayer.SteamId();
                if (steamId == 0) continue;

                _playerOnlineTimeDb.IncrementPlayerOnlineTime(steamId, (double) IntervalSecs / 3600);
                var onlineTime = _playerOnlineTimeDb.GetPlayerOnlineTime(steamId);

                var faction = factionList.FirstOrDefault(f => f.Members.ContainsKey(playerId));
                var factionTag = faction?.Tag ?? "<single>";
                factions.Increment(factionTag);

                var playerName = onlinePlayer.DisplayName;
                playerName = _nameConflictSolver.GetSafeName(playerName, steamId);

                TorchInfluxDbWriter
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
                TorchInfluxDbWriter
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
            }

            var totalOnlineTime = _playerOnlineTimeDb.GetTotalOnlineTime();

            TorchInfluxDbWriter
                .Measurement("server")
                .Field("players", onlinePlayers.Length)
                .Field("online_time", totalOnlineTime)
                .Write();

            _playerOnlineTimeDb.WriteToDb();
        }
    }
}