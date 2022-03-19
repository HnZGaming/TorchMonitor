using System;
using System.Threading.Tasks;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public abstract class ProfilerMonitorBase<T> : IIntervalListener
    {
        protected static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        protected readonly ITorchMonitorGeneralConfig _config;

        protected ProfilerMonitorBase(ITorchMonitorGeneralConfig config)
        {
            _config = config;
        }

        protected abstract int SamplingSeconds { get; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % SamplingSeconds != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            using (var profiler = MakeProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));
                profiler.MarkEnd();

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        protected abstract BaseProfiler<T> MakeProfiler();
        protected abstract void OnProfilingFinished(BaseProfilerResult<T> result);
    }
}