using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;

namespace TorchMonitor.Monitors
{
    public class VoxelMonitor : IIntervalListener
    {
        public bool Enabled { get; set; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart % 600 != 0) return;

            var voxels = MyEntities
                .GetEntities()
                .Select(e => e as MyVoxelBase)
                .Where(e => e != null)
                .Where(e => !e.Closed)
                .ToArray();

            var planets = voxels.OfType<MyPlanet>().ToArray();
            var asteroidCount = voxels.Length - planets.Length;

            TorchInfluxDbWriter
                .Measurement("voxels_asteroids_total")
                .Field("count", asteroidCount)
                .Write();

            TorchInfluxDbWriter
                .Measurement("voxels_planets_total")
                .Field("count", planets.Length)
                .Write();
        }
    }
}