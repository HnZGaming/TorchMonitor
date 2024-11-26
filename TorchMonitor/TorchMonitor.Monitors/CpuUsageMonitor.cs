using System;
using System.Diagnostics;
using InfluxDb.Torch;
using Intervals;

namespace TorchMonitor.Monitors
{
    public sealed class CpuUsageMonitor : IIntervalListener
    {
        const int Interval = 10;

        readonly PerformanceCounter[] _cpuCounters;
        readonly float[] _buffer;

        public bool Enabled { get; set; }

        public CpuUsageMonitor()
        {
            // Get the number of processors (cores)
            var coreCount = Environment.ProcessorCount;

            // Create an array of PerformanceCounter for each core
            _cpuCounters = new PerformanceCounter[coreCount];
            _buffer = new float[coreCount];

            for (var i = 0; i < coreCount; i++)
            {
                _cpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", $"{i}");
            }
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;

            for (var i = 0; i < _cpuCounters.Length; i++)
            {
                _buffer[i] += _cpuCounters[i].NextValue();
            }

            if (intervalsSinceStart % Interval == 0)
            {
                // Retrieve and display CPU usage for each core
                for (var i = 0; i < _buffer.Length; i++)
                {
                    var cpuUsage = _buffer[i] / Interval;
                    _buffer[i] = 0f;

                    TorchInfluxDbWriter
                        .Measurement("resource_cpu")
                        .Tag("core", $"{i}")
                        .Field("percentage", cpuUsage)
                        .Write();
                }
            }
        }
    }
}