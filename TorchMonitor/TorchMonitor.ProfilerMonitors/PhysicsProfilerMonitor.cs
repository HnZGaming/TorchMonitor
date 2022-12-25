using System;
using System.Threading.Tasks;
using Havok;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsProfilerMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public bool Enabled { get; set; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % TorchMonitorConfig.Instance.PhysicsInterval != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            using (var profiler = new PhysicsProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                await VRageUtils.MoveToGameLoop();

                profiler.MarkStart();

                for (var i = 0; i < TorchMonitorConfig.Instance.PhysicsFrameCount; i++)
                {
                    await VRageUtils.MoveToGameLoop();
                }

                profiler.MarkEnd();

                var result = profiler.GetResult();
                ProcessResult(result);
            }
        }

        void ProcessResult(BaseProfilerResult<HkWorld> result)
        {
            foreach (var (world, entity) in result.GetTopEntities(TorchMonitorConfig.Instance.PhysicsMaxClusterCount))
            {
                // this usually doesn't happen but just in case
                if (!PhysicsUtils.TryGetHeaviestGrid(world, out var heaviestGrid)) continue;

                heaviestGrid.BigOwners.TryGetFirst(out var ownerId);
                var faction = MySession.Static.Factions.GetPlayerFaction(ownerId);
                var factionTag = faction?.Tag ?? "<n/a>";
                var gridName = heaviestGrid.DisplayName.OrNull() ?? "<no name>";
                var mainMs = entity.MainThreadTime / result.TotalFrameCount;

                TorchInfluxDbWriter
                    .Measurement("profiler_physics_grids")
                    .Tag("grid", $"[{factionTag}] {gridName}")
                    .Field("main_ms", mainMs)
                    .Write();
            }
        }
    }
}