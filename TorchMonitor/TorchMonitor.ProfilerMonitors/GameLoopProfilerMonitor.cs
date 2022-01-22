using InfluxDb.Torch;
using Profiler.Basics;
using Profiler.Core;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class GameLoopProfilerMonitor : ProfilerMonitorBase<ProfilerCategory>
    {
        public GameLoopProfilerMonitor(ITorchMonitorGeneralConfig config) : base(config)
        {
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<ProfilerCategory> MakeProfiler()
        {
            return new GameLoopProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<ProfilerCategory> result)
        {
            var frameMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.Frame, 0);
            var lockMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.Lock, 0);
            var updateMs = (float) result.GetMainThreadTickMsOrElse(ProfilerCategory.Update, 0);
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
                .Field("frame", frameMs / result.TotalFrameCount)
                .Field("wait", lockMs / result.TotalFrameCount)
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