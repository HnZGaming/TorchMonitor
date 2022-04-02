using System;
using InfluxDb.Torch;
using Profiler.Basics;
using VRage.Game.Components;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class SessionComponentsProfilerMonitor : ProfilerMonitorBase<MySessionComponentBase>
    {
        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MySessionComponentBase> MakeProfiler()
        {
            return new SessionComponentsProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MySessionComponentBase> result)
        {
            foreach (var (comp, entity) in result.GetTopEntities())
            {
                var ms = (float) entity.MainThreadTime / result.TotalFrameCount;

                var type = comp.GetType();
                var name = TorchMonitorConfig.Instance.MonitorSessionComponentNamespace
                    ? $"{type.Namespace}/{type.Name}"
                    : type.Name;

                TorchInfluxDbWriter
                    .Measurement("profiler_game_loop_session_components")
                    .Tag("comp_name", name)
                    .Field("main_ms", ms)
                    .Write();
            }
        }
    }
}