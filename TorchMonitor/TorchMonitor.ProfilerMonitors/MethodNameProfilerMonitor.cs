using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class MethodNameProfilerMonitor : ProfilerMonitorBase<string>
    {
        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<string> MakeProfiler()
        {
            return new MethodNameProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<string> result)
        {
            foreach (var (name, entity) in result.GetTopEntities())
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_method_names")
                    .Tag("method_name", name)
                    .Field("main_ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", (float) entity.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float) entity.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}