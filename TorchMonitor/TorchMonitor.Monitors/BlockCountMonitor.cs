using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed class BlockCountMonitor : IIntervalListener
    {
        const int MaxMonitoredCount = 5;

        readonly IMonitorGeneralConfig _config;
        readonly Dictionary<long, int> _playerBlockCounts;
        readonly Dictionary<string, int> _factionBlockCounts;

        public BlockCountMonitor(
            IMonitorGeneralConfig config)
        {
            _config = config;
            _playerBlockCounts = new Dictionary<long, int>();
            _factionBlockCounts = new Dictionary<string, int>();
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

            // per player/faction
            {
                _playerBlockCounts.Clear();
                _factionBlockCounts.Clear();

                foreach (var grid in allGrids)
                foreach (var block in grid.CubeBlocks)
                {
                    var ownerId = GetOwnerId(block);
                    _playerBlockCounts.Increment(ownerId);
                }

                var topPlayerBlockCounts = _playerBlockCounts.OrderByDescending(p => p.Value).Take(MaxMonitoredCount);
                var allIds = MySession.Static.Players.GetAllIdentities().ToDictionary(id => id.IdentityId, id => id.DisplayName);
                allIds[0] = "<nobody>";
                foreach (var (id, blockCount) in topPlayerBlockCounts)
                {
                    if (!allIds.TryGetValue(id, out var playerName)) continue;

                    TorchInfluxDbWriter
                        .Measurement("blocks_players")
                        .Tag("name", playerName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                foreach (var (idId, blockCount) in _playerBlockCounts)
                {
                    var faction = MySession.Static.Factions.TryGetPlayerFaction(idId);
                    if (faction == null) continue;

                    _factionBlockCounts.Increment(faction.Name, blockCount);
                }

                var topFactionBlockCounts = _factionBlockCounts.OrderByDescending(p => p.Value).Take(MaxMonitoredCount);
                foreach (var (factionName, blockCount) in topFactionBlockCounts)
                {
                    TorchInfluxDbWriter
                        .Measurement("blocks_factions")
                        .Tag("name", factionName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                _playerBlockCounts.Clear();
                _factionBlockCounts.Clear();
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