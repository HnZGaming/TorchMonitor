using System.Collections.Generic;
using System.Linq;
using InfluxDB.Client.Writes;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Server.InfluxDb;
using Torch.Server.Utils;
using TorchMonitor.Utils;

namespace TorchMonitor.Business.Monitors
{
    public sealed class FactionConcealmentMonitor : IIntervalListener
    {
        public interface IConfig
        {
            int CollectIntervalSecs { get; }
            int WriteIntervalSecs { get; }
            string FactionTag { get; }
        }
        
        readonly InfluxDbClient _client;
        readonly List<MyCubeGrid> _collectedGrids;
        readonly IConfig _config;

        public FactionConcealmentMonitor(
            InfluxDbClient client,
            IConfig config)
        {
            _client = client;
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
            var points = new List<PointData>();

            foreach (var grid in _collectedGrids)
            {
                var activeBlockCount = grid.IsConcealed() ? 0 : grid.BlocksCount;
                var point = _client
                    .MakePointIn("faction_grids")
                    .Tag("faction_tag", _config.FactionTag)
                    .Tag("grid_name", grid.DisplayName)
                    .Field("active_block_count", activeBlockCount);

                points.Add(point);
            }

            {
                var totalCount = _collectedGrids.Count;
                var activeCount = _collectedGrids.Count(g => !g.IsConcealed());

                var point = _client
                    .MakePointIn("faction_grids_count")
                    .Tag("faction_tag", _config.FactionTag)
                    .Field("total_count", totalCount)
                    .Field("active_count", activeCount);
                

                points.Add(point);
            }

            _client.WritePoints(points.ToArray());
        }
    }
}