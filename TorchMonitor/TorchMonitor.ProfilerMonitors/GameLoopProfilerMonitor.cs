using System;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Monitors;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class GameLoopProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public GameLoopProfilerMonitor(IMonitorGeneralConfig config)
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
            var profiler = new GameLoopProfiler();
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<ProfilerCategory> result)
        {
            var updateMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.Update, 0);
            var waitMs = result.TotalTime - updateMs;
            var updateNetworkMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateNetwork, 0);
            var updateReplMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateReplication, 0);
            var updateSessionCompsMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateSessionComponents, 0);
            var updateSessionCompsAllMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateSessionComponentsAll, 0);
            var updateSessionCompsOtherMs = updateSessionCompsAllMs - updateSessionCompsMs;
            var updateGpsMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateGps, 0);
            var updateParallelWaitMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.UpdateParallelWait, 0);
            var updateOtherMs = updateMs - updateNetworkMs - updateReplMs - updateSessionCompsAllMs - updateGpsMs - updateParallelWaitMs;

            TorchInfluxDbWriter
                .Measurement("profiler_game_loop")
                .Field("tick", result.TotalFrameCount)
                .Field("frame", result.TotalTime / result.TotalFrameCount)
                .Field("wait", waitMs / result.TotalFrameCount)
                .Field("update", updateMs / result.TotalFrameCount)
                .Field("update_network", updateNetworkMs / result.TotalFrameCount)
                .Field("update_replication", updateReplMs / result.TotalFrameCount)
                .Field("update_session_components", updateSessionCompsMs / result.TotalFrameCount)
                .Field("update_session_components_all", updateSessionCompsAllMs / result.TotalFrameCount)
                .Field("update_session_components_other", updateSessionCompsOtherMs / result.TotalFrameCount)
                .Field("update_gps", updateGpsMs / result.TotalFrameCount)
                .Field("update_parallel_wait", updateParallelWaitMs / result.TotalFrameCount)
                .Field("update_other", updateOtherMs / result.TotalFrameCount)
                .Write();
        }
    }
}