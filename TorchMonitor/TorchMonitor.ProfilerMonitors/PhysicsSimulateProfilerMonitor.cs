using InfluxDb.Torch;
using Profiler.Basics;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsSimulateProfilerMonitor : ProfilerMonitorBase<string>
    {
        public PhysicsSimulateProfilerMonitor(ITorchMonitorGeneralConfig config) : base(config)
        {
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<string> MakeProfiler()
        {
            return new PhysicsSimulateProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<string> result)
        {
            foreach (var (methodName, entity) in result.GetTopEntities())
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_physics_simulate")
                    .Tag("method_name", methodName)
                    .Field("main_ms", (float)entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}