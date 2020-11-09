using System.Linq;
using InfluxDb;
using Sandbox.Game.Entities;

namespace TorchMonitor.Business.Monitors
{
    public class AsteroidMonitor : IIntervalListener
    {
        int? _lastAsteroidCount;

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            var asteroidCount = MyEntities.GetEntities()
                .Count(e => e is MyVoxelBase voxel &&
                            !(e is MyPlanet) &&
                            !voxel.Closed);

            var asteroidCountDelta = asteroidCount - _lastAsteroidCount ?? 0;
            _lastAsteroidCount = asteroidCount;

            InfluxDbPointFactory
                .Measurement("asteroids")
                .Field("count", asteroidCount)
                .Field("count_delta", asteroidCountDelta)
                .Write();
        }
    }
}