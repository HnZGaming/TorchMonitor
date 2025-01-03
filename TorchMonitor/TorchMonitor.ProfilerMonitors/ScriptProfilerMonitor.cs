﻿using System;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using TorchMonitor.Monitors;
using TorchMonitor.Utils;
using Utils.Torch;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class ScriptProfilerMonitor : ProfilerMonitorBase<MyProgrammableBlock>
    {
        const int MaxDisplayCount = 4;
        readonly NameConflictSolver<long> _nameConflictSolver;

        public ScriptProfilerMonitor(NameConflictSolver<long> nameConflictSolver)
        {
            _nameConflictSolver = nameConflictSolver;
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MyProgrammableBlock> MakeProfiler()
        {
            return new UserScriptProfiler(GameEntityMask.Empty);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MyProgrammableBlock> result)
        {
            foreach (var (pb, entity) in result.GetTopEntities(MaxDisplayCount))
            {
                var grid = pb?.GetParentEntityOfType<MyCubeGrid>();
                if (grid == null) return;

                var gridName = _nameConflictSolver.GetSafeName(grid.DisplayName, grid.EntityId);

                TorchInfluxDbWriter
                    .Measurement("profiler_scripts")
                    .Tag("grid_name", gridName)
                    .Tag("block_name", pb.DisplayNameText ?? "<anon>")
                    .Field("main_ms", (float)entity.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", (float)entity.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", (float)entity.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}