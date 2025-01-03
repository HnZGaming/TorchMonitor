﻿using InfluxDb.Torch;
using Profiler.Basics;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsSimulateProfilerMonitor : ProfilerMonitorBase<string>
    {
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
                    .Field("sub_ms", (float)entity.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float)entity.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}