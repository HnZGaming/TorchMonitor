using System.Linq;
using InfluxDb;
using Intervals;
using Sandbox.Game.Entities;

namespace TorchMonitor.Monitors
{
    public class VoxelMonitor : IIntervalListener
    {
        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 600 != 0) return;

            var voxels = MyEntities
                .GetEntities()
                .Select(e => e as MyVoxelBase)
                .Where(e => e != null)
                .Where(e => !e.Closed)
                .ToArray();

            var planets = voxels.OfType<MyPlanet>().ToArray();
            var asteroidCount = voxels.Length - planets.Length;

            InfluxDbPointFactory
                .Measurement("voxels_asteroids_total")
                .Field("count", asteroidCount)
                .Write();

            InfluxDbPointFactory
                .Measurement("voxels_planets_total")
                .Field("count", planets.Length)
                .Write();
        }
    }
}