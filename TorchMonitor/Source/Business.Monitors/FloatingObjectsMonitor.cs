using System.Linq;
using Sandbox.Game.Entities;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business.Monitors
{
    public class FloatingObjectsMonitor : IIntervalListener
    {
        readonly InfluxDbClient _client;

        public FloatingObjectsMonitor(InfluxDbClient client)
        {
            _client = client;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            var count = MyEntities.GetEntities().Count(e => e is MyFloatingObject);
            var point = _client.MakePointIn("floating_objects")
                .Field("count", count);

            _client.WritePoints(point);
        }
    }
}