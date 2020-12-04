using System;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Monitors;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class MethodNameProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public MethodNameProfilerMonitor(IMonitorGeneralConfig config)
        {
            _config = config;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % SamplingSeconds != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            using (var profiler = new MethodNameProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<string> result)
        {
            foreach (var (name, entity) in result.GetTopEntities())
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_method_names")
                    .Tag("method_name", name)
                    .Field("ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}