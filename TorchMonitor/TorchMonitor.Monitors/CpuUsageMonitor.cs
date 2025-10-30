using System;
using System.Diagnostics;
using InfluxDb.Torch;
using Intervals;

namespace TorchMonitor.Monitors
{
    public sealed class CpuUsageMonitor : IIntervalListener
    {
        const int Interval = 10;

        readonly Process _process;
        TimeSpan _lastProc;
        DateTime _lastTime;
        bool _doneFirstInterval;
        double _procSum;

        public bool Enabled { get; set; }

        public CpuUsageMonitor()
        {
            _process = Process.GetCurrentProcess();
            _lastTime = DateTime.UtcNow;
            _lastProc = _process.TotalProcessorTime;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;

            var time = DateTime.UtcNow;
            var proc = _process.TotalProcessorTime;

            if (!_doneFirstInterval)
            {
                _doneFirstInterval = true;
            }
            else
            {
                var deltaProc = (proc - _lastProc).TotalMilliseconds;
                var deltaTime = (time - _lastTime).TotalMilliseconds;
                _procSum += deltaProc / deltaTime;
            }

            _lastTime = time;
            _lastProc = proc;

            if (intervalsSinceStart % Interval == 0)
            {
                var avgProcPercentage = (_procSum / Interval) * 100;
                _procSum = 0;

                TorchInfluxDbWriter
                    .Measurement("resource_cpu_self")
                    .Field("percentage", avgProcPercentage)
                    .Write();
            }
        }
    }
}