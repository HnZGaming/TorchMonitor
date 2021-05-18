using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed class BlockCountMonitor : IIntervalListener
    {
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

                foreach (var grid in allGrids)
                foreach (var block in grid.CubeBlocks)
                {
                    _playerBlockCounts.Increment(block.OwnerId);
                }

                var allIds = MySession.Static.Players.GetAllIdentities().ToDictionary(id => id.IdentityId);
                foreach (var (idId, blockCount) in _playerBlockCounts)
                {
                    if (!allIds.TryGetValue(idId, out var id)) continue;

                    TorchInfluxDbWriter
                        .Measurement("blocks_players")
                        .Tag("name", id.DisplayName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                _playerBlockCounts.Clear();
                _factionBlockCounts.Clear();

                foreach (var (idId, blockCount) in _playerBlockCounts)
                {
                    var faction = MySession.Static.Factions.TryGetPlayerFaction(idId);
                    if (faction != null)
                    {
                        _factionBlockCounts.Increment(faction.Name, blockCount);
                    }
                }

                foreach (var (factionName, blockCount) in _factionBlockCounts)
                {
                    TorchInfluxDbWriter
                        .Measurement("blocks_factions")
                        .Tag("name", factionName.Replace("\\", "-"))
                        .Field("block_count", blockCount)
                        .Write();
                }

                _factionBlockCounts.Clear();
            }
        }
    }
}