using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Utils.General;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class GridProfilerMonitor : IIntervalListener
    {
        public interface IConfig
        {
            bool DetailOutput { get; }
        }

        const int SamplingSeconds = 10;
        const int MaxDisplayCount = 4;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _generalConfig;
        readonly IConfig _gridProfilerConfig;
        readonly NameConflictSolver _nameConflictSolver;

        public GridProfilerMonitor(
            IMonitorGeneralConfig generalConfig,
            IConfig gridProfilerConfig,
            NameConflictSolver nameConflictSolver)
        {
            _generalConfig = generalConfig;
            _gridProfilerConfig = gridProfilerConfig;
            _nameConflictSolver = nameConflictSolver;
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < _generalConfig.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % SamplingSeconds != 0) return;

            Profile().Forget(Log);
        }

        async Task Profile()
        {
            var gameEntityMask = new GameEntityMask(null, null, null);
            using (var profiler = new GridProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<MyCubeGrid> result)
        {
            foreach (var (grid, entity) in result.GetTopEntities(MaxDisplayCount))
            {
                TorchInfluxDbWriter
                    .Measurement("profiler")
                    .Tag("grid_name", GridToResultText(grid))
                    .Field("main_ms", (float) entity.MainThreadTime / result.TotalFrameCount)
                    .Write();
            }
        }

        string GridToResultText(MyCubeGrid grid)
        {
            var safeGridName = GetSafeGridName(grid);

            if (!_gridProfilerConfig.DetailOutput)
            {
                return safeGridName;
            }

            if (!grid.BigOwners.Any())
            {
                return $"{safeGridName} (no owners)";
            }

            var names = new List<string>();

            foreach (var bigOwner in grid.BigOwners)
            {
                var id = MySession.Static.Players.TryGetIdentity(bigOwner);
                if (id == null) continue;

                var faction = MySession.Static.Factions.GetPlayerFaction(bigOwner);

                var playerName = id.DisplayName;
                var factionTag = faction?.Tag ?? "<single>";

                names.Add($"{playerName} [{factionTag}]");
            }

            return $"{safeGridName} ({string.Join(", ", names)})";
        }

        string GetSafeGridName(MyCubeGrid grid)
        {
            var gridName = grid.DisplayName;
            if (string.IsNullOrEmpty(gridName))
            {
                gridName = "<noname>";
            }

            return _nameConflictSolver.GetSafeName(gridName, grid.EntityId);
        }
    }
}