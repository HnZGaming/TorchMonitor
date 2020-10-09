using System.Collections.Generic;
using System.Linq;
using InfluxDB.Client.Writes;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business
{
    public sealed class WelderMonitor : IMonitor
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        readonly InfluxDbClient _client;
        readonly List<IMyShipWelder> _welderBlocks;

        public WelderMonitor(InfluxDbClient client)
        {
            _client = client;
            _welderBlocks = new List<IMyShipWelder>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 60) return;

            if (intervalsSinceStart % 60 == 0)
            {
                _welderBlocks.Clear();

                var blocks = MyCubeGridGroups.Static.Logical.Groups
                    .SelectMany(g => g.Nodes.Select(n => n.NodeData))
                    .Where(g => !g.IsConcealed())
                    .SelectMany(g => g.CubeBlocks)
                    .ToArray();

                foreach (var block in blocks)
                {
                    var player = MySession.Static.Players.TryGetSteamId(block.OwnerId);
                    if (player == 0) continue; // npc

                    if (!(block.FatBlock is IMyShipWelder welderBlock)) continue; // not a welder block

                    _welderBlocks.Add(welderBlock);
                }
            }

            if (intervalsSinceStart % 10 == 0)
            {
                var activeWelderCounts = new Dictionary<ulong, int>();
                var totalActiveWelderCount = 0;
                foreach (var welderBlock in _welderBlocks)
                {
                    if (welderBlock.Closed) continue; // destroyed
                    if (welderBlock.IsConcealed()) continue; // concealed
                    if (!welderBlock.Enabled) continue; // not active

                    var player = MySession.Static.Players.TryGetSteamId(welderBlock.OwnerId);
                    if (player == 0) continue; // npc

                    activeWelderCounts.TryGetValue(player, out var welderCount);
                    activeWelderCounts[player] = welderCount + 1;

                    totalActiveWelderCount += 1;
                }

                var points = new List<PointData>();
                foreach (var (player, activeWelderCount) in activeWelderCounts)
                {
                    if (activeWelderCount == 0) continue; // not using any welders

                    var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(player);

                    var point = _client.MakePointIn("welders_per_player")
                        .Tag("player_id", $"{player}")
                        .Tag("player_name", playerName)
                        .Field("active_welder_count", activeWelderCount);

                    points.Add(point);
                }

                var totalCountPoint = _client.MakePointIn("welders")
                    .Field("total_active_count", totalActiveWelderCount);

                points.Add(totalCountPoint);

                _client.WritePoints(points.ToArray());
            }
        }
    }
}