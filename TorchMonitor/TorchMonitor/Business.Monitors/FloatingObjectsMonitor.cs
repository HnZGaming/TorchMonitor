using System.Linq;
using Sandbox.Game.Entities;
using InfluxDb;

namespace TorchMonitor.Business.Monitors
{
    public class FloatingObjectsMonitor : IIntervalListener
    {
        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            var count = MyEntities.GetEntities().Count(e => e is MyFloatingObject);

            InfluxDbPointFactory
                .Measurement("floating_objects")
                .Field("count", count)
                .Write();
        }
    }
}