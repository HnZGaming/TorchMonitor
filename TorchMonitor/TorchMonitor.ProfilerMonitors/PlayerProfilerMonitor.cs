﻿using System;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Monitors;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PlayerProfilerMonitor : ProfilerMonitorBase<MyIdentity>
    {
        readonly NameConflictSolver<ulong> _nameConflictSolver;

        public PlayerProfilerMonitor(NameConflictSolver<ulong> nameConflictSolver)
        {
            _nameConflictSolver = nameConflictSolver;
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MyIdentity> MakeProfiler()
        {
            return new PlayerProfiler(GameEntityMask.Empty);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MyIdentity> result)
        {
            foreach (var (player, entity) in result.GetTopEntities(10))
            {
                var playerName = player.DisplayName;
                var steamId = MySession.Static.Players.TryGetSteamId(player.IdentityId);
                playerName = _nameConflictSolver.GetSafeName(playerName, steamId);

                TorchInfluxDbWriter
                    .Measurement("profiler_players")
                    .Tag("player_name", playerName)
                    .Field("main_ms", entity.MainThreadTime / result.TotalFrameCount)
                    .Field("sub_ms", entity.OffThreadTime / result.TotalFrameCount)
                    .Field("total_ms", entity.TotalTime / result.TotalFrameCount)
                    .Write();
            }
        }
    }
}