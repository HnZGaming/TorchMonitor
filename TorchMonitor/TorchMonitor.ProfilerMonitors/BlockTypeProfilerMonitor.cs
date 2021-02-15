using System;
using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class BlockTypeProfilerMonitor : ProfilerMonitorBase<Type>
    {
        const int MaxDisplayCount = 10;

        public BlockTypeProfilerMonitor(IMonitorGeneralConfig config) : base(config)
        {
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<Type> MakeProfiler()
        {
            var mask = new GameEntityMask(null, null, null);
            return new BlockTypeProfiler(mask);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<Type> result)
        {
            foreach (var (type, entry) in result.GetTopEntities(MaxDisplayCount))
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_block_types")
                    .Tag("block_type", type.Name)
                    .Field("main_ms", (float) entry.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}