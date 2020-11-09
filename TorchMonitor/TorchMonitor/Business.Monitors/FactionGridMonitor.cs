using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using InfluxDb;
using TorchUtils;

namespace TorchMonitor.Business.Monitors
{
    public sealed class FactionGridMonitor : IIntervalListener
    {
        public interface IConfig
        {
            int CollectIntervalSecs { get; }
            int WriteIntervalSecs { get; }
            string FactionTag { get; }
        }

        readonly List<MyCubeGrid> _collectedGrids;
        readonly IConfig _config;

        public FactionGridMonitor(IConfig config)
        {
            _config = config;
            _collectedGrids = new List<MyCubeGrid>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 120) return;

            if (intervalsSinceStart % _config.CollectIntervalSecs == 0)
            {
                CollectGrids();
            }

            if (intervalsSinceStart % _config.WriteIntervalSecs == 0)
            {
                Write();
            }
        }

        void CollectGrids()
        {
            var factionMembers = MySession.Static.Factions.Factions.Values
                .Where(f => f.Tag == _config.FactionTag)
                .SelectMany(f => f.Members.Keys);

            var factionMembersSet = new HashSet<long>(factionMembers);

            var grids = MyCubeGridGroups.Static.Logical.Groups
                .SelectMany(g => g.Nodes.Select(n => n.NodeData))
                .Where(g => factionMembersSet.ContainsAny(g.BigOwners));

            _collectedGrids.Clear();
            _collectedGrids.AddRange(grids);
        }

        void Write()
        {
            foreach (var grid in _collectedGrids)
            {
                var activeBlockCount = grid.IsConcealed() ? 0 : grid.BlocksCount;

                InfluxDbPointFactory
                    .Measurement("faction_grids")
                    .Tag("faction_tag", _config.FactionTag)
                    .Tag("grid_name", grid.DisplayName)
                    .Field("active_block_count", activeBlockCount)
                    .Write();
            }

            {
                var totalCount = _collectedGrids.Count;
                var activeCount = _collectedGrids.Count(g => !g.IsConcealed());

                InfluxDbPointFactory
                    .Measurement("faction_grids_count")
                    .Tag("faction_tag", _config.FactionTag)
                    .Field("total_count", totalCount)
                    .Field("active_count", activeCount)
                    .Write();
            }
        }
    }
}