using System;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Utils;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PlayerProfilerMonitor : ProfilerMonitorBase<MyIdentity>
    {
        readonly NameConflictSolver<ulong> _nameConflictSolver;

        public PlayerProfilerMonitor(IMonitorGeneralConfig config, NameConflictSolver<ulong> nameConflictSolver) : base(config)
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