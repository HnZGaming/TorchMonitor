using System;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class ScriptProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        const int MaxDisplayCount = 4;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;
        readonly NameConflictSolver<long> _nameConflictSolver;

        public ScriptProfilerMonitor(
            IMonitorGeneralConfig config,
            NameConflictSolver<long> nameConflictSolver)
        {
            _config = config;
            _nameConflictSolver = nameConflictSolver;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _config.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % SamplingSeconds != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            var gameEntityMask = new GameEntityMask(null, null, null);
            using (var profiler = new UserScriptProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<MyProgrammableBlock> result)
        {
            foreach (var (pb, entity) in result.GetTopEntities(MaxDisplayCount))
            {
                var grid = pb?.GetParentEntityOfType<MyCubeGrid>();
                if (grid == null) return;

                var gridName = _nameConflictSolver.GetSafeName(grid.DisplayName, grid.EntityId);
                var mainMs = (float) entity.MainThreadTime / result.TotalFrameCount;

                TorchInfluxDbWriter
                    .Measurement("profiler_scripts")
                    .Tag("grid_name", gridName)
                    .Field("main_ms", mainMs)
                    .Write();
            }
        }
    }
}