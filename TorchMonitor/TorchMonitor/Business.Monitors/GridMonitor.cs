using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using TorchDatabaseIntegration.InfluxDB;
using TorchUtils;
using VRage.Game.Entity;

namespace TorchMonitor.Business.Monitors
{
    public class GridMonitor : IIntervalListener
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly Regex NamePattern = new Regex(@"^(Static|Large|Small)\s(Grid|Ship)\s\d+$");

        int? _lastGroupCount;
        int? _lastBlockCount;

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 120) return;

            if (intervalsSinceStart % 60 == 0)
            {
                var groups = MyCubeGridGroups.Static.Logical.Groups
                    .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                    .ToArray();

                var activeGroups = groups.Where(gr => gr.Any(g => !g.IsConcealed())).ToArray();

                // total
                {
                    var groupCount = groups.Length;
                    var groupCountDelta = (groupCount - _lastGroupCount) ?? 0;
                    _lastGroupCount = groupCount;

                    var blockCount = groups.SelectMany(g => g).Sum(g => g.BlocksCount);
                    var blockCountDelta = (blockCount - _lastBlockCount) ?? 0;
                    _lastBlockCount = blockCount;

                    var deletableGridCount = groups.Count(g => IsGridGroupDeletable(g));

                    InfluxDbPointFactory
                        .Measurement("grids")
                        .Field("total", groupCount)
                        .Field("total_delta", groupCountDelta)
                        .Field("deletable", deletableGridCount)
                        .Field("total_blocks", blockCount)
                        .Field("total_blocks_delta", blockCountDelta)
                        .Field("active_total", activeGroups.Length)
                        .Write();
                }

                // each active grid
                Parallel.ForEach(activeGroups, activeGroup =>
                {
                    var biggestGrid = activeGroup.GetBiggestGrid();
                    var groupName = biggestGrid.DisplayName;
                    var multiBlockGrids = activeGroup.Where(g => !IsWheelGrid(g)).ToArray();

                    var conveyorCount = 0;
                    var sorterCount = 0;
                    var endpointCount = 0;
                    var productionBlockCount = 0;
                    var programmableBlockCount = 0;
                    var shipToolCount = 0;
                    foreach (var multiBlockGrid in multiBlockGrids)
                    foreach (var slimBlock in multiBlockGrid.CubeBlocks)
                    {
                        var block = slimBlock.FatBlock;

                        // skip inactive
                        if (block is IMyFunctionalBlock functionalBlock)
                        {
                            if (!functionalBlock.IsWorking) continue;
                            if (!functionalBlock.Enabled) continue;
                        }

                        if (block is IMyConveyor ||
                            block is IMyConveyorTube)
                        {
                            conveyorCount += 1;
                        }

                        if (block is MyConveyorSorter)
                        {
                            sorterCount += 1;
                        }

                        if (block is IMyConveyorEndpointBlock endpointBlock)
                        {
                            endpointCount += endpointBlock.ConveyorEndpoint.GetLineCount();
                        }

                        if (block is IMyProductionBlock)
                        {
                            productionBlockCount += 1;
                        }

                        if (block is IMyProgrammableBlock)
                        {
                            programmableBlockCount += 1;
                        }

                        if (block is IMyShipToolBase)
                        {
                            shipToolCount += 1;
                        }
                    }

                    var factionTag = biggestGrid.BigOwners
                        .Select(o => MySession.Static.Factions.TryGetPlayerFaction(o))
                        .FirstOrDefault()
                        ?.Tag ?? "<single>";

                    InfluxDbPointFactory
                        .Measurement("active_grids")
                        .Tag("grid_name", groupName)
                        .Tag("faction_tag", factionTag)
                        .Field("pcu", multiBlockGrids.Sum(g => g.BlocksPCU))
                        .Field("block_count", multiBlockGrids.Sum(g => g.BlocksCount))
                        .Field("subgrid_count", multiBlockGrids.Length - 1)
                        .Field("conveyor_count", conveyorCount)
                        .Field("sorter_count", sorterCount)
                        .Field("endpoint_count", endpointCount)
                        .Field("production_block_count", productionBlockCount)
                        .Field("programmable_block_count", programmableBlockCount)
                        .Field("ship_tool_count", shipToolCount)
                        .Write();
                });
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

        static bool IsWheelGrid(MyCubeGrid grid)
        {
            var blocks = grid.CubeBlocks;
            if (blocks.Count != 1) return false;

            var block = blocks.ElementAt(0);
            return block.FatBlock is MyWheel;
        }
    }
}