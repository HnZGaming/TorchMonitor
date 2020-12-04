using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class GridMonitor : IIntervalListener
    {
        readonly IMonitorGeneralConfig _config;
        readonly NameConflictSolver _nameConflictSolver;

        int? _lastGroupCount;
        int? _lastBlockCount;

        public GridMonitor(
            IMonitorGeneralConfig config,
            NameConflictSolver nameConflictSolver)
        {
            _config = config;
            _nameConflictSolver = nameConflictSolver;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 60 != 0) return;

            var allGroups = MyCubeGridGroups.Static.Logical.Groups
                .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                .ToArray();

            var activeGroups = allGroups
                .Where(gr => gr.Any(g => !g.IsConcealed()))
                .ToArray();

            // total
            {
                var groupCount = allGroups.Length;
                var groupCountDelta = (groupCount - _lastGroupCount) ?? 0;
                _lastGroupCount = groupCount;

                var blockCount = allGroups.SelectMany(g => g).Sum(g => g.BlocksCount);
                var blockCountDelta = (blockCount - _lastBlockCount) ?? 0;
                _lastBlockCount = blockCount;

                var activeBlockCount = activeGroups.SelectMany(g => g).Sum(g => g.BlocksCount);

                var totalPcu = allGroups.SelectMany(g => g).Sum(g => g.BlocksPCU);
                var activePcu = activeGroups.SelectMany(g => g).Sum(g => g.BlocksPCU);

                TorchInfluxDbWriter
                    .Measurement("construction_all")
                    .Field("total_grid_count", groupCount)
                    .Field("total_block_count", blockCount)
                    .Field("active_block_count", activeBlockCount)
                    .Field("total_pcu", totalPcu)
                    .Field("active_grid_count", activeGroups.Length)
                    .Field("active_pcu", activePcu)
                    .Write();

                TorchInfluxDbWriter
                    .Measurement("construction_delta")
                    .Field("grid_count_delta", groupCountDelta)
                    .Field("block_count_delta", blockCountDelta)
                    .Write();
            }

            // get the number of online members per each faction
            var factionMemberCounts = new Dictionary<string, int>();
            foreach (var onlinePlayer in MySession.Static.Players.GetOnlinePlayers().ToArray())
            {
                if (onlinePlayer == null) continue;

                var faction = MySession.Static.Factions.TryGetPlayerFaction(onlinePlayer.PlayerId());
                var factionTag = faction?.Tag ?? "<single>";

                factionMemberCounts.Increment(factionTag);
            }

            var allBlockCategoryCounts = new BlockCategoryCounter();

            // active grids
            Parallel.ForEach(activeGroups, activeGroup =>
            {
                var biggestGrid = activeGroup.GetBiggestGrid();
                if (biggestGrid?.Closed ?? true) return;

                var groupName = _nameConflictSolver.GetSafeName(biggestGrid.DisplayName, biggestGrid.EntityId);

                var factionTag = biggestGrid.BigOwners
                    .Select(o => MySession.Static.Factions.TryGetPlayerFaction(o))
                    .FirstOrDefault()
                    ?.Tag ?? "<single>";

                var factionMemberCount = factionMemberCounts.TryGetValue(factionTag, out var c) ? c : 0;

                TorchInfluxDbWriter
                    .Measurement("active_grids")
                    .Tag("grid_name", groupName)
                    .Tag("faction_tag", factionTag)
                    .Field("faction_member_count", factionMemberCount)
                    .Field("pcu", activeGroup.Sum(g => g.BlocksPCU))
                    .Field("block_count", activeGroup.Sum(g => g.BlocksCount))
                    .Write();

                var blockCategoryCounts = new BlockCategoryCounter();
                foreach (var grid in activeGroup)
                foreach (var slimBlock in grid.CubeBlocks)
                {
                    var block = slimBlock?.FatBlock;
                    if (block == null) continue;
                    if (block.Closed) continue;

                    // skip disabled (turned off) blocks
                    if (block is IMyFunctionalBlock functionalBlock)
                    {
                        if (!functionalBlock.IsWorking) continue;
                        if (!functionalBlock.Enabled) continue;
                    }

                    allBlockCategoryCounts.Count(block);
                    blockCategoryCounts.Count(block);
                }

                if (blockCategoryCounts.Any())
                {
                    var blockCategoryCountsPoint =
                        TorchInfluxDbWriter
                            .Measurement("block_category_count_per_active_grid")
                            .Tag("grid_name", groupName)
                            .Tag("faction_tag", factionTag)
                            .Field("faction_member_count", factionMemberCount);

                    foreach (var (categoryName, count) in blockCategoryCounts.Counts)
                    {
                        blockCategoryCountsPoint.Field(categoryName, count);
                    }

                    blockCategoryCountsPoint.Write();
                }
            });

            if (allBlockCategoryCounts.Any())
            {
                var allBlockCategoryCountPoint = TorchInfluxDbWriter
                    .Measurement("block_category_count_all_active_grids");

                foreach (var (categoryName, count) in allBlockCategoryCounts.Counts)
                {
                    allBlockCategoryCountPoint.Field(categoryName, count);
                }

                allBlockCategoryCountPoint.Write();
            }
        }
    }
}