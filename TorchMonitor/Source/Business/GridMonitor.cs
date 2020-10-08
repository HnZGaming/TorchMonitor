using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InfluxDB.Client.Writes;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Server.InfluxDb;
using TorchUtils.Utils;
using VRage.Game.Entity;

namespace TorchMonitor.Business
{
    public class GridMonitor : IMonitor
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        static readonly Regex NamePattern = new Regex(@"^(Static|Large|Small)\s(Grid|Ship)\s\d+$");
        readonly InfluxDbClient _client;

        int? _lastGroupCount;
        int? _lastBlockCount;
        int? _lastNonStaticGroupCount;

        public GridMonitor(InfluxDbClient client)
        {
            _client = client;
            _lastGroupCount = 0;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 60) return;

            var startTimeOrNull = (DateTime?) null;

            if (intervalsSinceStart % 10 == 0)
            {
                startTimeOrNull = DateTime.UtcNow;

                var groups = MyCubeGridGroups.Static.Logical.Groups
                    .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                    .ToArray();

                var groupCount = groups.Length;
                var groupCountDelta = (groupCount - _lastGroupCount) ?? 0;
                _lastGroupCount = groupCount;

                var blockCount = groups.SelectMany(g => g).Sum(g => g.BlocksCount);
                var blockCountDelta = (blockCount - _lastBlockCount) ?? 0;
                _lastBlockCount = blockCount;

                var mobileGroupCount = groups.Count(g => !g.GetTopGrid().IsStatic);
                var mobileGroupCountDelta = (mobileGroupCount - _lastNonStaticGroupCount) ?? 0;
                _lastNonStaticGroupCount = mobileGroupCount;

                var deletableGridCount = groups.Count(g => IsGridGroupDeletable(g));

                var activeGrids = groups
                    .SelectMany(g => g)
                    .Where(g => !g.IsConcealed())
                    .Select(g => (g.IsStatic, g.BlocksCount))
                    .ToArray();

                var staticSyncBlockCount = activeGrids.Where(g => g.IsStatic).Select(g => g.BlocksCount).Sum();
                var mobileSyncBlockCount = activeGrids.Where(g => !g.IsStatic).Select(g => g.BlocksCount).Sum();

                var pcu = MySession.Static.TotalPCU;
                var sessionPcu = MySession.Static.TotalSessionPCU;
                var activePcu = groups
                    .SelectMany(g => g)
                    .Where(g => !g.IsConcealed())
                    .Select(g => g.BlocksPCU)
                    .Sum();

                var point = _client.MakePointIn("grids")
                    .Field("total", groupCount)
                    .Field("total_delta", groupCountDelta)
                    .Field("deletable", deletableGridCount)
                    .Field("total_blocks", blockCount)
                    .Field("total_blocks_delta", blockCountDelta)
                    .Field("mobile_group_count", mobileGroupCount)
                    .Field("mobile_group_count_delta", mobileGroupCountDelta)
                    .Field("static_active_blocks", staticSyncBlockCount)
                    .Field("mobile_active_blocks", mobileSyncBlockCount)
                    .Field("total_pcu", pcu)
                    .Field("total_session_pcu", sessionPcu)
                    .Field("active_pcu", activePcu);

                _client.WritePoints(point);
            }

            if (intervalsSinceStart % 60 == 0)
            {
                var points = new List<PointData>();

                var grids = MyCubeGridGroups.Static.Logical.Groups
                    .SelectMany(g => g.Nodes.Select(n => n.NodeData))
                    .ToArray();

                var players = new HashSet<long>();
                var totalBlockCountsPerPlayer = new Dictionary<long, int>();
                var activeBlockCountsPerPlayer = new Dictionary<long, int>();
                var totalPcuPerPlayer = new Dictionary<long, int>();
                var activePcuPerPlayer = new Dictionary<long, int>();

                foreach (var grid in grids)
                {
                    var isActive = !grid.IsConcealed();

                    foreach (var block in grid.CubeBlocks)
                    {
                        var player = block.OwnerId;
                        players.Add(player);

                        totalBlockCountsPerPlayer.TryGetValue(player, out var totalBlockCount);
                        totalBlockCountsPerPlayer[player] = totalBlockCount + 1;

                        totalPcuPerPlayer.TryGetValue(player, out var totalPcu);
                        totalPcuPerPlayer[player] = totalPcu + block.BlockDefinition.PCU;

                        if (isActive)
                        {
                            activeBlockCountsPerPlayer.TryGetValue(player, out var activeBlockCount);
                            activeBlockCountsPerPlayer[player] = activeBlockCount + 1;

                            activePcuPerPlayer.TryGetValue(player, out var activePcu);
                            activePcuPerPlayer[player] = activePcu + block.BlockDefinition.PCU;
                        }
                    }
                }

                _logger.Trace($"all players with blocks: {players.ToStringSeq()}");

                const int MaxPlayerCount = 10;

                var topPlayers = new HashSet<long>();

                topPlayers.UnionWith(
                    totalBlockCountsPerPlayer
                        .OrderByDescending(p => p.Value)
                        .Select(p => p.Key)
                        .Take(MaxPlayerCount));

                topPlayers.UnionWith(
                    activeBlockCountsPerPlayer
                        .OrderByDescending(p => p.Value)
                        .Select(p => p.Key)
                        .Take(MaxPlayerCount));

                topPlayers.UnionWith(
                    totalPcuPerPlayer
                        .OrderByDescending(p => p.Value)
                        .Select(p => p.Key)
                        .Take(MaxPlayerCount));

                topPlayers.UnionWith(
                    activePcuPerPlayer
                        .OrderByDescending(p => p.Value)
                        .Select(p => p.Key)
                        .Take(MaxPlayerCount));

                _logger.Trace($"top players: {topPlayers.ToStringSeq()}");

                foreach (var playerId in topPlayers)
                {
                    var steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    if (steamId == 0) continue; // npc

                    var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
                    if (string.IsNullOrEmpty(playerName)) continue; // idk why this happens

                    var totalBlockCount = totalBlockCountsPerPlayer[playerId];
                    var activeBlockCount = activeBlockCountsPerPlayer.TryGetValue(playerId, out var c) ? c : 0;
                    var totalPcu = totalPcuPerPlayer.TryGetValue(playerId, out var q) ? q : 0;
                    var activePcu = activePcuPerPlayer.TryGetValue(playerId, out var r) ? r : 0;

                    var point = _client.MakePointIn("grids_per_player")
                        .Tag("steam_id", $"{steamId}")
                        .Tag("player_name", playerName)
                        .Field("total_block_count", totalBlockCount)
                        .Field("active_block_count", activeBlockCount)
                        .Field("total_pcu", totalPcu)
                        .Field("active_pcu", activePcu);

                    points.Add(point);

                    _logger.Trace($"writing for player: {steamId} '{playerName}' {totalBlockCount} {activeBlockCount}");
                }

                _client.WritePoints(points.ToArray());
            }

            if (startTimeOrNull is DateTime startTime)
            {
                var endTime = DateTime.UtcNow;
                _logger.Trace($"Grid monitor process time: {(endTime - startTime).TotalMilliseconds:0.00}ms");
            }
        }

        static bool IsGridGroupDeletable(IEnumerable<MyCubeGrid> group)
        {
            // Unowned grids should be deleted
            var owners = group.SelectMany(g => g.BigOwners.Concat(g.SmallOwners));
            if (!owners.Any()) return true;

            // Unnamed grids should be deleted
            if (group.All(g => IsUnnamed(g))) return true;

            return false;
        }

        static bool IsUnnamed(MyEntity grid)
        {
            return NamePattern.IsMatch(grid.DisplayName);
        }
    }
}