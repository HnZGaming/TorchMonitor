﻿using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class NetworkEventProfilerMonitor : ProfilerMonitorBase<string>
    {
        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<string> MakeProfiler()
        {
            return new NetworkEventProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<string> result)
        {
            foreach (var (methodName, entry) in result.GetTopEntities())
            {
                var eventName = methodName.Split('#')[1];
                TorchInfluxDbWriter
                    .Measurement("profiler_network_events")
                    .Tag("site_name", eventName)
                    .Field("main_ms", (float)entry.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", (float)entry.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float)entry.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}