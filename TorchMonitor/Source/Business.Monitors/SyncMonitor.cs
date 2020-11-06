using System.Linq;
using Sandbox.Game.Multiplayer;
using TorchDatabaseIntegration.InfluxDB;

namespace TorchMonitor.Business.Monitors
{
    public class SyncMonitor : IIntervalListener
    {
        const int IntervalsPerWrite = 5;

        readonly float[] _simSpeeds;

        public SyncMonitor()
        {
            _simSpeeds = new float[IntervalsPerWrite];
        }

        public void OnInterval(int intervalsSinceStart)
        {
            var simSpeed = Sync.ServerSimulationRatio;
            _simSpeeds[intervalsSinceStart % _simSpeeds.Length] = simSpeed;

            if (intervalsSinceStart < 120) return; // the first some minutes are noisy
            if (intervalsSinceStart % IntervalsPerWrite != 0) return;

            var maxSimSpeed = _simSpeeds.Max();
            var minSimSpeed = _simSpeeds.Min();
            var avgSimSpeed = _simSpeeds.Average();

            InfluxDbPointFactory
                .Measurement("server_sync")
                .Field("sim_speed", avgSimSpeed)
                .Field("sim_speed_min", minSimSpeed)
                .Field("sim_speed_max", maxSimSpeed)
                .Write();
        }
    }
}