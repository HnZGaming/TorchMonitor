using System.Linq;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Sandbox.Engine.Multiplayer;
using TorchMonitor.Utils;
using Utils.General;
using VRage.Network;
using VRage.Replication;

namespace TorchMonitor.Monitors
{
    public sealed class ClientPingMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        readonly GeoLocationCollection _locations;

        public ClientPingMonitor(GeoLocationCollection locations)
        {
            _locations = locations;
        }

        public bool Enabled { get; set; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 10 != 0) return;

            ((MyReplicationServer)MyMultiplayer.ReplicationLayer).GetClientPings(out var pings);
            foreach (var (steamId, ping) in pings.Dictionary)
            {
                if (!_locations.TryGetLocation(steamId, out var location))
                {
                    _locations.Load(steamId).Forget(Log);
                    continue;
                }

                if (ping == 0) continue; // client loading into the game

                var countryName = $"{location.CountryName ?? "<unknown>"}";
                var regionName = $"{location.RegionName ?? "<unknown>"}";

                var playerName = ((MyDedicatedServerBase)MyMultiplayerMinimalBase.Instance).GetMemberName(steamId);
                playerName ??= $"{steamId}";

                TorchInfluxDbWriter
                    .Measurement("players_pings")
                    .Tag("player_name", playerName)
                    .Tag("country", countryName)
                    .Tag("region", $"{countryName}/{regionName}")
                    .Field("ping", ping)
                    .Write();

                Log.Debug($"ping: {playerName} {regionName} {ping}");
            }
        }
    }
}