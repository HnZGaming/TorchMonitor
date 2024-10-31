using System;
using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class BlockTypeProfilerMonitor : ProfilerMonitorBase<Type>
    {
        const int MaxDisplayCount = 10;

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<Type> MakeProfiler()
        {
            return new BlockTypeProfiler(GameEntityMask.Empty);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<Type> result)
        {
            foreach (var (type, entry) in result.GetTopEntities(MaxDisplayCount))
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_block_types")
                    .Tag("block_type", type.Name)
                    .Field("main_ms", (float) entry.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", (float) entry.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float) entry.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}