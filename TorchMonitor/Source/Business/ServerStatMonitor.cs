using System;
using System.Linq;
using Sandbox.Game.World;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business
{
    public class ServerStatMonitor : IMonitor
    {
        const int IntervalsPerWrite = 5;

        readonly InfluxDbClient _client;
        readonly float[] _simSpeeds;

        float? _lastSimTimeSinceLaunch;
        DateTime? _lastUpdateTime;

        public ServerStatMonitor(InfluxDbClient client)
        {
            _client = client;
            _lastSimTimeSinceLaunch = 1f;
            _simSpeeds = new float[IntervalsPerWrite];
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % IntervalsPerWrite == 0 &&
                intervalsSinceStart > 40) // don't monitor first seconds
            {
                var maxSimSpeed = _simSpeeds.Max();
                var minSimSpeed = _simSpeeds.Min();
                var avgSimSpeed = _simSpeeds.Average();

                var point = _client.MakePointIn("server")
                    .Field("sim_speed", avgSimSpeed)
                    .Field("sim_speed_min", minSimSpeed)
                    .Field("sim_speed_max", maxSimSpeed);

                _client.WritePoints(point);
            }

            var simTimeSinceLaunch = MySession.Static.SessionSimSpeedServer;
            var simTimeSinceLaunchDelta = simTimeSinceLaunch - _lastSimTimeSinceLaunch;
            var simUpdateDuration = (DateTime.Now - _lastUpdateTime)?.TotalSeconds;
            var simSpeed = (float) (simTimeSinceLaunchDelta / simUpdateDuration ?? 1f);

            _simSpeeds[intervalsSinceStart % IntervalsPerWrite] = simSpeed;
            _lastUpdateTime = DateTime.Now;
            _lastSimTimeSinceLaunch = simTimeSinceLaunch;
        }
    }
}