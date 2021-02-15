using System;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PlayerProfilerMonitor : ProfilerMonitorBase<MyIdentity>
    {
        readonly NameConflictSolver _nameConflictSolver;

        public PlayerProfilerMonitor(
            IMonitorGeneralConfig config,
            NameConflictSolver nameConflictSolver) : base(config)
        {
            _nameConflictSolver = nameConflictSolver;
        }

        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<MyIdentity> MakeProfiler()
        {
            var mask = new GameEntityMask(null, null, null);
            return new PlayerProfiler(mask);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<MyIdentity> result)
        {
            foreach (var (player, entity) in result.GetTopEntities())
            {
                var playerName = player.DisplayName;
                playerName = _nameConflictSolver.GetSafeName(playerName, player.IdentityId);

                var mainMs = entity.MainThreadTime / result.TotalFrameCount;

                TorchInfluxDbWriter
                    .Measurement("profiler_players")
                    .Tag("player_name", playerName)
                    .Field("main_ms", mainMs)
                    .Write();
            }
        }
    }
}