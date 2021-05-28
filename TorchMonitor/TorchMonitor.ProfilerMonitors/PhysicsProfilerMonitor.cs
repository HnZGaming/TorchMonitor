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
        public interface IConfig
        {
            bool PhysicsEnabled { get; }
            int PhysicsInterval { get; }
            int PhysicsFrameCount { get; }
            int PhysicsMaxClusterCount { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;
        readonly IConfig _physicsConfig;

        public PhysicsProfilerMonitor(IMonitorGeneralConfig config, IConfig physicsConfig)
        {
            _config = config;
            _physicsConfig = physicsConfig;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!_physicsConfig.PhysicsEnabled) return;
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % _physicsConfig.PhysicsInterval != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            using (var profiler = new PhysicsProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                await GameLoopObserver.MoveToGameLoop();

                profiler.MarkStart();

                for (var i = 0; i < _physicsConfig.PhysicsFrameCount; i++)
                {
                    await GameLoopObserver.MoveToGameLoop();
                }

                profiler.MarkEnd();

                var result = profiler.GetResult();
                ProcessResult(result);
            }
        }

        void ProcessResult(BaseProfilerResult<HkWorld> result)
        {
            foreach (var ((_, entity), index) in result.GetTopEntities(_physicsConfig.PhysicsMaxClusterCount).Indexed())
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