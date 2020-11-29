using System;
using System.Threading.Tasks;
using InfluxDb;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Monitors;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class BlockTypeProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        const int MaxDisplayCount = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public BlockTypeProfilerMonitor(IMonitorGeneralConfig config)
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
            using (var profiler = new BlockTypeProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<Type> result)
        {
            foreach (var (type, entry) in result.GetTopEntities(MaxDisplayCount))
            {
                InfluxDbPointFactory
                    .Measurement("profiler_block_types")
                    .Tag("block_type", type.Name)
                    .Field("main_ms", (float) entry.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}