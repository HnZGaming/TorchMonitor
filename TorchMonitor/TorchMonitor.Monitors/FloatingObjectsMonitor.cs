using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Entities;
using TorchMonitor.Utils;

namespace TorchMonitor.Monitors
{
    public class FloatingObjectsMonitor : IIntervalListener
    {
        public bool Enabled { get; set; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 10 != 0) return;

            var count = MyEntities.GetEntities().Count(e => e is MyFloatingObject);

            TorchInfluxDbWriter
                .Measurement("floating_objects")
                .Field("count", count)
                .Write();
        }
    }
}