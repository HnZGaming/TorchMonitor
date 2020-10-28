using System.Linq;
using Sandbox.Game.Entities;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business.Monitors
{
    public class AsteroidMonitor : IIntervalListener
    {
        readonly InfluxDbClient _client;
        int? _lastAsteroidCount;

        public AsteroidMonitor(InfluxDbClient client)
        {
            _client = client;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            var asteroidCount = MyEntities.GetEntities()
                .Count(e => e is MyVoxelBase voxel &&
                            !(e is MyPlanet) &&
                            !voxel.Closed);

            var asteroidCountDelta = asteroidCount - _lastAsteroidCount ?? 0;
            _lastAsteroidCount = asteroidCount;

            var point = _client.MakePointIn("asteroids")
                .Field("count", asteroidCount)
                .Field("count_delta", asteroidCountDelta);

            _client.WritePoints(point);
        }
    }
}