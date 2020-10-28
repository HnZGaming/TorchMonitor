using System.Diagnostics;
using Torch.Server.InfluxDb;

namespace TorchMonitor.Business.Monitors
{
    public class CpuUsageMonitor : IIntervalListener
    {
        readonly InfluxDbClient _client;
        readonly PerformanceCounter _cpuUsageCounter;

        public CpuUsageMonitor(InfluxDbClient client)
        {
            _client = client;
            _cpuUsageCounter = new PerformanceCounter(
                "Process", "% Processor Time",
                Process.GetCurrentProcess().ProcessName,
                true);
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < 20) return;
            if (intervalsSinceStart % 10 != 0) return;

            var cpuUsage = _cpuUsageCounter.NextValue() / 100;
            var point = _client.MakePointIn("resource")
                .Field("cpu", cpuUsage);

            _client.WritePoints(point);
        }
    }
}