using System;
using System.Threading.Tasks;
using InfluxDb;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.Entities;
using TorchMonitor.Monitors;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class GridProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        const int MaxDisplayCount = 4;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public GridProfilerMonitor(IMonitorGeneralConfig config)
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
            var gameEntityMask = new GameEntityMask(null, null, null);
            using (var profiler = new GridProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<MyCubeGrid> result)
        {
            foreach (var (grid, entity) in result.GetTopEntities(MaxDisplayCount))
            {
                InfluxDbPointFactory
                    .Measurement("profiler")
                    .Tag("grid_name", grid.DisplayName)
                    .Field("main_ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}