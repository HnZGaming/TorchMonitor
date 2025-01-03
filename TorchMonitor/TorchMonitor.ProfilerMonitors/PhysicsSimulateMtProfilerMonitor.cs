﻿using System;
using Havok;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PhysicsSimulateMtProfilerMonitor : ProfilerMonitorBase<HkWorld>
    {
        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<HkWorld> MakeProfiler()
        {
            return new PhysicsSimulateMtProfiler();
        }

        protected override void OnProfilingFinished(BaseProfilerResult<HkWorld> result)
        {
            var noneCount = 0;
            foreach (var (world, entity) in result.GetTopEntities(5))
            {
                // this usually doesn't happen but just in case
                string factionTag;
                string gridName;
                if (PhysicsUtils.TryGetHeaviestGrid(world, out var heaviestGrid))
                {
                    heaviestGrid.BigOwners.TryGetFirst(out var ownerId);
                    var faction = MySession.Static.Factions.GetPlayerFaction(ownerId);
                    factionTag = faction?.Tag ?? "n/a";
                    gridName = heaviestGrid.DisplayName.OrNull() ?? $"none ({noneCount++})";
                }
                else
                {
                    factionTag = "n/a";
                    gridName = $"none ({noneCount++})";
                }

                TorchInfluxDbWriter
                    .Measurement("profiler_physics_simulate_mt")
                    .Tag("grid", $"[{factionTag}] {gridName}")
                    .Field("main_ms", entity.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", entity.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", entity.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}