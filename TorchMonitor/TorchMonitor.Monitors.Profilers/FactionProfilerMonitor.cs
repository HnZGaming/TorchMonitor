using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDb;
using Intervals;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace TorchMonitor.Monitors.Profilers
{
    public sealed class FactionProfilerMonitor : IIntervalListener
    {
        const int SamplingSeconds = 10;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IMonitorGeneralConfig _config;

        public FactionProfilerMonitor(IMonitorGeneralConfig config)
        {
            _config = config;
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
            using (var profiler = new FactionProfiler(gameEntityMask))
            using (ProfilerResultQueue.Profile(profiler))
            {
                profiler.MarkStart();
                await Task.Delay(TimeSpan.FromSeconds(SamplingSeconds));

                var result = profiler.GetResult();
                OnProfilingFinished(result);
            }
        }

        void OnProfilingFinished(BaseProfilerResult<IMyFaction> result)
        {
            // get online players per faction
            var onlineFactions = new Dictionary<string, int>();
            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers();
            foreach (var onlinePlayer in onlinePlayers)
            {
                var faction = MySession.Static.Factions.TryGetPlayerFaction(onlinePlayer.PlayerId());
                if (faction == null) continue;

                onlineFactions.Increment(faction.Tag);
            }

            foreach (var (faction, entity) in result.GetTopEntities())
            {
                onlineFactions.TryGetValue(faction.Tag, out var onlinePlayerCount);
                onlinePlayerCount = Math.Max(1, onlinePlayerCount); // fix zero division
                var mainMs = entity.MainThreadTime / result.TotalFrameCount;
                var mainMsPerMember = mainMs / onlinePlayerCount;

                InfluxDbPointFactory
                    .Measurement("profiler_factions")
                    .Tag("faction_tag", faction.Tag)
                    .Field("main_ms", mainMs)
                    .Field("main_ms_per_member", mainMsPerMember)
                    .Write();
            }
        }
    }
}