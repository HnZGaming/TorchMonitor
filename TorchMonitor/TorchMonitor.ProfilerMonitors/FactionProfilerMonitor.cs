using System;
using System.Collections.Generic;
using InfluxDb.Torch;
using Profiler.Basics;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace TorchMonitor.ProfilerMonitors
{
    public sealed class FactionProfilerMonitor : ProfilerMonitorBase<IMyFaction>
    {
        protected override int SamplingSeconds => 10;

        protected override BaseProfiler<IMyFaction> MakeProfiler()
        {
            return new FactionProfiler(GameEntityMask.Empty);
        }

        protected override void OnProfilingFinished(BaseProfilerResult<IMyFaction> result)
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

                TorchInfluxDbWriter
                    .Measurement("profiler_factions")
                    .Tag("faction_tag", faction.Tag)
                    .Field("main_ms", entity.MainThreadTime / result.TotalFrameCount)
                    .Field("main_ms_per_member", entity.MainThreadTime / result.TotalFrameCount / onlinePlayerCount)
                    .Field("sub_ms", entity.OffThreadTime / result.TotalFrameCount)
                    .Field("sub_ms_per_member", entity.OffThreadTime / result.TotalFrameCount / onlinePlayerCount)
                    .Field("total_ms", entity.TotalTime / result.TotalFrameCount)
                    .Field("total_ms_per_member", entity.TotalTime / result.TotalFrameCount / onlinePlayerCount)
                    .Write();
            }
        }
    }
}