using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class PingMonitor : IIntervalListener, IDisposable
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly ITorchMonitorGeneralConfig _config;
        readonly Ping _ping;
        bool _disposed;

        const string Host = "google.com";

        public PingMonitor(ITorchMonitorGeneralConfig config)
        {
            _config = config;
            _ping = new Ping();
        }

        public void Dispose()
        {
            _ping.Dispose();
            _disposed = true;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 10 != 0) return;

            SendPing().Forget(Log);
        }

        async Task SendPing()
        {
            await VRageUtils.MoveToThreadPool();
            if (_disposed) return;

            var response = _ping.Send(Host);
            if (response == null) return; // shouldn't happen
            if (_disposed) return;
            
            var success = response.Status == IPStatus.Success;
            var ms = response.RoundtripTime;
            
            TorchInfluxDbWriter
                .Measurement("ping")
                .Field("success", success)
                .Field("ms", ms)
                .Write();
        }
    }
}