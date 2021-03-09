using System;
using System.Threading.Tasks;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class PlayerProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;
        readonly NameConflictSolver<ulong> _nameConflictSolver;

        public PlayerProfilerMonitor(
            IMonitorGeneralConfig config,
            NameConflictSolver<ulong> nameConflictSolver)
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
            using (var profiler = new PlayerProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<MyIdentity> result)
        {
            foreach (var (player, entity) in result.GetTopEntities())
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