using System;
using System.Threading.Tasks;
using InfluxDb;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Monitors;
using Utils.General;
using VRage.Game.Components;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class SessionComponentsProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public SessionComponentsProfilerMonitor(IMonitorGeneralConfig config)
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
            using (var profiler = new SessionComponentsProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<MySessionComponentBase> result)
        {
            foreach (var (comp, entity) in result.GetTopEntities())
            {
                InfluxDbPointFactory
                    .Measurement("profiler_game_loop_session_components")
                    .Tag("comp_name", comp.GetType().Name)
                    .Field("main_ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}