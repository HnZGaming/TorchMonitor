using System;
using System.Threading.Tasks;
using Havok;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsProfilerMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public PhysicsProfilerMonitor(IMonitorGeneralConfig config)
        {
            _config = config;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 60 != 0) return;

            Profile().Forget(Log);
        }

        static async Task Profile()
        {
            using (var profiler = new PhysicsProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                await GameLoopObserver.MoveToGameLoop();

                profiler.MarkStart();

                for (var i = 0; i < 10; i++)
                {
                    await GameLoopObserver.MoveToGameLoop();
                }

                profiler.MarkEnd();

                var result = profiler.GetResult();
                ProcessResult(result);
            }
        }

        static void ProcessResult(BaseProfilerResult<HkWorld> result)
        {
            foreach (var ((_, entity), index) in result.GetTopEntities(5).Indexed())
            {
                var mainMs = entity.MainThreadTime / result.TotalFrameCount;

                TorchInfluxDbWriter
                    .Measurement("profiler_physics")
                    .Tag("index", $"{index}")
                    .Field("main_ms", mainMs)
                    .Write();
            }
        }
    }
}