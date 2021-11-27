using System;
using System.Linq;
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
using VRage.Game.ModAPI;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsProfilerMonitor : IIntervalListener
    {
        public interface IConfig
        {
            bool PhysicsEnabled { get; }
            int PhysicsInterval { get; }
            int PhysicsFrameCount { get; }
            int PhysicsMaxClusterCount { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly ITorchMonitorGeneralConfig _config;
        readonly IConfig _physicsConfig;

        public PhysicsProfilerMonitor(ITorchMonitorGeneralConfig config, IConfig physicsConfig)
        {
            _config = config;
            _physicsConfig = physicsConfig;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!_physicsConfig.PhysicsEnabled) return;
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % _physicsConfig.PhysicsInterval != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            using (var profiler = new PhysicsProfiler())
            using (ProfilerResultQueue.Profile(profiler))
            {
                await GameLoopObserver.MoveToGameLoop();

                profiler.MarkStart();

                for (var i = 0; i < _physicsConfig.PhysicsFrameCount; i++)
                {
                    await GameLoopObserver.MoveToGameLoop();
                }

                profiler.MarkEnd();

                var result = profiler.GetResult();
                ProcessResult(result);
            }
        }

        void ProcessResult(BaseProfilerResult<HkWorld> result)
        {
            foreach (var (world, entity) in result.GetTopEntities(_physicsConfig.PhysicsMaxClusterCount))
            {
                // this usually doesn't happen but just in case
                if (!TryGetHeaviestGrid(world, out var heaviestGrid)) continue;

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

        static bool TryGetHeaviestGrid(HkWorld world, out IMyCubeGrid heaviestGrid)
        {
            var grids = world
                .GetEntities()
                .Where(e => e is IMyCubeGrid)
                .Cast<IMyCubeGrid>()
                .Where(e => e.IsTopMostParent())
                .ToArray();

            if (!grids.Any())
            {
                heaviestGrid = null;
                return false;
            }

            heaviestGrid = grids.OrderByDescending(g => g.Physics.Mass).First();
            return true;
        }
    }
}