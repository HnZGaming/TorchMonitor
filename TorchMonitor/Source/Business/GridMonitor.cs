using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InfluxDB.Client.Writes;
using NLog;
using Sandbox.Game.Entities;
using Torch.Server.InfluxDb;
using TorchMonitor.Utils;
using VRage.Game.Entity;

namespace TorchMonitor.Business
{
    public class GridMonitor : IMonitor
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly Regex NamePattern = new Regex(@"^(Static|Large|Small)\s(Grid|Ship)\s\d+$");
        readonly InfluxDbClient _client;

        int? _lastGroupCount;
        int? _lastBlockCount;

        public GridMonitor(InfluxDbClient client)
        {
            _client = client;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 120) return;

            if (intervalsSinceStart % 20 == 0)
            {
                var points = new List<PointData>();

                var groups = MyCubeGridGroups.Static.Logical.Groups
                    .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                    .ToArray();

                // total
                {
                    var groupCount = groups.Length;
                    var groupCountDelta = (groupCount - _lastGroupCount) ?? 0;
                    _lastGroupCount = groupCount;

                    var blockCount = groups.SelectMany(g => g).Sum(g => g.BlocksCount);
                    var blockCountDelta = (blockCount - _lastBlockCount) ?? 0;
                    _lastBlockCount = blockCount;

                    var deletableGridCount = groups.Count(g => IsGridGroupDeletable(g));

                    var point = _client
                        .MakePointIn("grids")
                        .Field("total", groupCount)
                        .Field("total_delta", groupCountDelta)
                        .Field("deletable", deletableGridCount)
                        .Field("total_blocks", blockCount)
                        .Field("total_blocks_delta", blockCountDelta);

                    points.Add(point);
                }

                // active
                {
                    foreach (var group in groups)
                    {
                        if (group.Any(g => g.IsConcealed())) continue;

                        var biggestGrid = GetBiggestGrid(group);
                        var groupName = biggestGrid.DisplayName;
                        var blockCount = group.Sum(g => g.BlocksCount);
                        var totalPcu = group.Sum(g => g.BlocksPCU);

                        var point = _client
                            .MakePointIn("active_grids")
                            .Tag("grid_name", groupName)
                            .Field("block_count", blockCount)
                            .Field("pcu", totalPcu);

                        points.Add(point);
                    }
                }

                _client.WritePoints(points.ToArray());
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

        static MyCubeGrid GetBiggestGrid(IEnumerable<MyCubeGrid> grids)
        {
            var myCubeGrid = (MyCubeGrid) null;
            var num = 0.0;
            foreach (var grid in grids)
            {
                var volume = grid.PositionComp.WorldAABB.Size.Volume;
                if (volume > num)
                {
                    num = volume;
                    myCubeGrid = grid;
                }
            }

            return myCubeGrid;
        }
    }
}