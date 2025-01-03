﻿using InfluxDb.Torch;
using Profiler.Basics;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class EntityTypeProfilerMonitor : ProfilerMonitorBase<string>
    {
        const int MaxDisplayCount = 10;

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<string> MakeProfiler()
        {
            return new EntityTypeProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<string> result)
        {
            foreach (var (name, entry) in result.GetTopEntities(MaxDisplayCount))
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_entity_types")
                    .Tag("entity_type", name)
                    .Field("main_ms", (float)entry.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", (float)entry.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float)entry.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}