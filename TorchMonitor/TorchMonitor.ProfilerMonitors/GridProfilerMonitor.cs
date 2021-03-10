using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class GridProfilerMonitor : ProfilerMonitorBase<MyCubeGrid>
    {
        public interface IConfig
        {
            bool DetailOutput { get; }
        }

        const int MaxDisplayCount = 4;
        readonly IConfig _gridProfilerConfig;
        readonly NameConflictSolver<long> _nameConflictSolver;

        public GridProfilerMonitor(
            IMonitorGeneralConfig generalConfig,
            IConfig gridProfilerConfig,
            NameConflictSolver<long> nameConflictSolver) : base(generalConfig)
        {
            _gridProfilerConfig = gridProfilerConfig;
            _nameConflictSolver = nameConflictSolver;
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MyCubeGrid> MakeProfiler()
        {
            var mask = new GameEntityMask(null, null, null);
            return new GridProfiler(mask);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MyCubeGrid> result)
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

            gridName = _nameConflictSolver.GetSafeName(gridName, grid.EntityId);

            return gridName;
        }
    }
}