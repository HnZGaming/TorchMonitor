using System.Linq;
using InfluxDb;
using Intervals;
using Sandbox.Game.Entities;

namespace TorchMonitor.Monitors
{
    public class FloatingObjectsMonitor : IIntervalListener
    {
        readonly IMonitorGeneralConfig _config;

        public FloatingObjectsMonitor(IMonitorGeneralConfig config)
        {
            _config = config;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 10 != 0) return;

            var count = MyEntities.GetEntities().Count(e => e is MyFloatingObject);

            InfluxDbPointFactory
                .Measurement("floating_objects")
                .Field("count", count)
                .Write();
        }
    }
}