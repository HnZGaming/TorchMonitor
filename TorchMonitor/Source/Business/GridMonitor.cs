using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InfluxDB.Client.Writes;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch;
using Torch.Server.InfluxDb;
using TorchUtils.Utils;
using VRage.Game.Entity;
using VRage.ModAPI;
using IMyEntity = VRage.ModAPI.IMyEntity;

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
                    .Where(g => !IsConcealed(g))
                    .Select(g => (g.IsStatic, g.BlocksCount))
                    .ToArray();

                var staticSyncBlockCount = activeGrids.Where(g => g.IsStatic).Select(g => g.BlocksCount).Sum();
                var mobileSyncBlockCount = activeGrids.Where(g => !g.IsStatic).Select(g => g.BlocksCount).Sum();

                var pcu = MySession.Static.TotalPCU;
                var sessionPcu = MySession.Static.TotalSessionPCU;
                var activePcu = groups
                    .SelectMany(g => g)
                    .Where(g => !IsConcealed(g))
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

            if (intervalsSinceStart % 20 == 0)
            {
                var groups = MyCubeGridGroups.Static.Logical.Groups
                    .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                    .ToArray();

                var players = new HashSet<long>();
                var totalBlockCountsPerPlayer = new Dictionary<long, int>();
                var activeBlockCountsPerPlayer = new Dictionary<long, int>();

                foreach (var grid in groups.SelectMany(g => g))
                {
                    var isActive = !IsConcealed(grid);

                    foreach (var block in grid.CubeBlocks)
                    {
                        players.Add(block.OwnerId);
                        totalBlockCountsPerPlayer.TryGetValue(block.OwnerId, out var totalCount);
                        totalBlockCountsPerPlayer[block.OwnerId] = totalCount + 1;

                        if (isActive)
                        {
                            activeBlockCountsPerPlayer.TryGetValue(block.OwnerId, out var activeCount);
                            activeBlockCountsPerPlayer[block.OwnerId] = activeCount + 1;
                        }
                    }
                }

                _logger.Trace($"all players with blocks: {players.ToStringSeq()}");

                var topPlayersWithTotalBlockCount = totalBlockCountsPerPlayer.OrderByDescending(p => p.Value).Select(p => p.Key).Take(15);
                var topPlayersWithActiveBlockCount = activeBlockCountsPerPlayer.OrderByDescending(p => p.Value).Select(p => p.Key).Take(15);
                var topPlayersWithSomeBlockCount = new HashSet<long>();
                topPlayersWithSomeBlockCount.UnionWith(topPlayersWithTotalBlockCount);
                topPlayersWithSomeBlockCount.UnionWith(topPlayersWithActiveBlockCount);

                _logger.Trace($"top players with blocks: {topPlayersWithSomeBlockCount.ToStringSeq()}");

                var points = new List<PointData>();
                foreach (var playerId in topPlayersWithSomeBlockCount)
                {
                    var steamId = MySession.Static.Players.TryGetSteamId(playerId);
                    if (steamId == 0) continue; // npc

                    var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);

                    var totalBlockCount = totalBlockCountsPerPlayer[playerId];

                    if (totalBlockCount == 0) continue; // empty data

                    var activeBlockCount = activeBlockCountsPerPlayer.TryGetValue(playerId, out var c) ? c : 0;

                    var point = _client.MakePointIn("grids_per_player")
                        .Tag("steam_id", $"{steamId}")
                        .Tag("player_name", playerName)
                        .Field("total_block_count", totalBlockCount)
                        .Field("active_block_count", activeBlockCount);

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

        static bool IsConcealed(IMyEntity entity)
        {
            // Concealment plugin uses `4` as a flag to prevent game from updating grids
            return (long) (entity.Flags & (EntityFlags) 4) != 0;
        }
    }
}