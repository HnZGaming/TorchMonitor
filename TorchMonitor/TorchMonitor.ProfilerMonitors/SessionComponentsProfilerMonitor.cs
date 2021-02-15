using System;
using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;
using VRage.Game.Components;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class SessionComponentsProfilerMonitor : ProfilerMonitorBase<MySessionComponentBase>
    {
        public SessionComponentsProfilerMonitor(IMonitorGeneralConfig config) : base(config)
        {
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MySessionComponentBase> MakeProfiler()
        {
            return new SessionComponentsProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MySessionComponentBase> result)
        {
            foreach (var (comp, entity) in result.GetTopEntities())
            {
                TorchInfluxDbWriter
                    .Measurement("profiler_game_loop_session_components")
                    .Tag("comp_name", comp.GetType().Name)
                    .Field("main_ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}