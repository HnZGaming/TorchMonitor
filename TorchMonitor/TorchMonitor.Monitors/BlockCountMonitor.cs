using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed class BlockCountMonitor : IIntervalListener
    {
        const int MaxMonitoredCount = 5;

        readonly ITorchMonitorGeneralConfig _config;

        public BlockCountMonitor(
            ITorchMonitorGeneralConfig config)
        {
            _config = config;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 60 != 0) return;

            var allGroups = MyCubeGridGroups.Static.Logical.Groups
                .Select(g => g.Nodes.Select(n => n.NodeData).ToArray())
                .ToArray();

            var allGrids = allGroups.SelectMany(g => g).ToArray();

            //total
            {
                var totalBlockCount = allGrids.Sum(g => g.BlocksCount);

                TorchInfluxDbWriter
                    .Measurement("blocks_all")
                    .Field("block_count", totalBlockCount)
                    .Write();
            }

            // per grid
            {
                var gridBlockCounts = new Dictionary<long, int>();
                foreach (var grid in allGrids)
                {
                    gridBlockCounts.Increment(grid.EntityId);
                }

                var topGridBlockCounts = gridBlockCounts.OrderByDescending(p => p.Value).Take(MaxMonitoredCount);
                var gridNames = allGrids.ToDictionary(p => p.EntityId, p => p.DisplayName);
                foreach (var (gridId, blockCount) in topGridBlockCounts)
                {
                    var gridName = gridNames[gridId];

                    TorchInfluxDbWriter
                        .Measurement("blocks_grids")
                        .Tag("name", gridName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }
            }

            // per player/faction
            {
                var playerBlockCounts = new Dictionary<long, int>();
                var factionBlockCounts = new Dictionary<string, int>();

                foreach (var grid in allGrids)
                foreach (var block in grid.CubeBlocks)
                {
                    var ownerId = GetOwnerId(block);
                    playerBlockCounts.Increment(ownerId);
                }

                var topPlayerBlockCounts = playerBlockCounts.OrderByDescending(p => p.Value).Take(MaxMonitoredCount);
                var playerNames = MySession.Static.Players.GetAllIdentities().ToDictionary(id => id.IdentityId, id => id.DisplayName);
                playerNames[0] = "<nobody>";
                foreach (var (id, blockCount) in topPlayerBlockCounts)
                {
                    if (!playerNames.TryGetValue(id, out var playerName)) continue;

                    TorchInfluxDbWriter
                        .Measurement("blocks_players")
                        .Tag("name", playerName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                foreach (var (idId, blockCount) in playerBlockCounts)
                {
                    var faction = MySession.Static.Factions.TryGetPlayerFaction(idId);
                    if (faction == null) continue;

                    factionBlockCounts.Increment(faction.Name, blockCount);
                }

                var topFactionBlockCounts = factionBlockCounts.OrderByDescending(p => p.Value).Take(MaxMonitoredCount);
                foreach (var (factionName, blockCount) in topFactionBlockCounts)
                {
                    TorchInfluxDbWriter
                        .Measurement("blocks_factions")
                        .Tag("name", factionName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                playerBlockCounts.Clear();
                factionBlockCounts.Clear();
            }
        }

        static long GetOwnerId(MySlimBlock block)
        {
            var ownerId = block.OwnerId;
            if (ownerId == 0)
            {
                ownerId = block.BuiltBy;
            }

            return ownerId;
        }
    }
}