using System.Linq;
using Sandbox.Game.Multiplayer;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business
{
    public class SyncMonitor : IMonitor
    {
        const int IntervalsPerWrite = 5;

        readonly InfluxDbClient _client;
        readonly float[] _simSpeeds;

        public SyncMonitor(InfluxDbClient client)
        {
            _client = client;
            _simSpeeds = new float[IntervalsPerWrite];
        }

        public void OnInterval(int intervalsSinceStart)
        {
            var simSpeed = Sync.ServerSimulationRatio;
            _simSpeeds[intervalsSinceStart % _simSpeeds.Length] = simSpeed;

            if (intervalsSinceStart % IntervalsPerWrite == 0 &&
                intervalsSinceStart > 40) // don't monitor first seconds
            {
                var maxSimSpeed = _simSpeeds.Max();
                var minSimSpeed = _simSpeeds.Min();
                var avgSimSpeed = _simSpeeds.Average();

                var point = _client.MakePointIn("server_sync")
                    .Field("sim_speed", avgSimSpeed)
                    .Field("sim_speed_min", minSimSpeed)
                    .Field("sim_speed_max", maxSimSpeed);

                _client.WritePoints(point);
            }
        }
    }
}