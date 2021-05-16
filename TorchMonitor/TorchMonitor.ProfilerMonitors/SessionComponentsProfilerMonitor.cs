using System;
using InfluxDb.Torch;
using Profiler.Basics;
using TorchMonitor.Utils;
using VRage.Game.Components;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class SessionComponentsProfilerMonitor : ProfilerMonitorBase<MySessionComponentBase>
    {
        readonly IConfig _myConfig;

        public interface IConfig
        {
            bool MonitorSessionComponentNamespace { get; }
        }

        public SessionComponentsProfilerMonitor(
            IMonitorGeneralConfig generalConfig,
            IConfig myConfig) : base(generalConfig)
        {
            _myConfig = myConfig;
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
                var ms = (float) entity.MainThreadTime / result.TotalFrameCount;

                var type = comp.GetType();
                var name = _myConfig.MonitorSessionComponentNamespace
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