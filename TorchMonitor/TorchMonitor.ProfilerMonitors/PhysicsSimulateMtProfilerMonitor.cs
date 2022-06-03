﻿using System;
using Havok;
using InfluxDb.Torch;
using NLog;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using VRage.Game.ModAPI;

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
            foreach (var (world, entity) in result.GetTopEntities(5))
            {
                IMyCubeGrid heaviestGrid;

                try
                {
                    // this usually doesn't happen but just in case
                    if (!PhysicsUtils.TryGetHeaviestGrid(world, out heaviestGrid)) continue;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    continue;
                }

                heaviestGrid.BigOwners.TryGetFirst(out var ownerId);
                var faction = MySession.Static.Factions.GetPlayerFaction(ownerId);
                var factionTag = faction?.Tag ?? "<n/a>";
                var gridName = heaviestGrid.DisplayName.OrNull() ?? "<no name>";
                var mainMs = entity.MainThreadTime / result.TotalFrameCount;

                TorchInfluxDbWriter
                    .Measurement("profiler_physics_simulate_mt")
                    .Tag("grid", $"[{factionTag}] {gridName}")
                    .Field("main_ms", mainMs)
                    .Write();
            }
        }
    }
}