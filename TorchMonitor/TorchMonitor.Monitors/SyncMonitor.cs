using System.Linq;
using InfluxDb.Torch;
using Intervals;
using Sandbox.Game.Multiplayer;

namespace TorchMonitor.Monitors
{
    public class SyncMonitor : IIntervalListener
    {
        const int IntervalsPerWrite = 5;

        readonly float[] _simSpeeds;
        readonly IMonitorGeneralConfig _config;

        public SyncMonitor(IMonitorGeneralConfig config)
        {
            _config = config;
            _simSpeeds = new float[IntervalsPerWrite];
        }

        public void OnInterval(int intervalsSinceStart)
        {
            var simSpeed = Sync.ServerSimulationRatio;
            _simSpeeds[intervalsSinceStart % _simSpeeds.Length] = simSpeed;

            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % IntervalsPerWrite != 0) return;

            var maxSimSpeed = _simSpeeds.Max();
            var minSimSpeed = _simSpeeds.Min();
            var avgSimSpeed = _simSpeeds.Average();

            TorchInfluxDbWriter
                .Measurement("server_sync")
                .Field("sim_speed", avgSimSpeed)
                .Field("sim_speed_min", minSimSpeed)
                .Field("sim_speed_max", maxSimSpeed)
                .Write();
        }
    }
}