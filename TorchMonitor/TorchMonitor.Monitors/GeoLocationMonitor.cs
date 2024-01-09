using System;
using System.Collections.Generic;
using System.Linq;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Sandbox.Game.World;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor.Monitors
{
    public sealed class GeoLocationMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly GeoLocationCollection _locations;

        public GeoLocationMonitor(GeoLocationCollection locations)
        {
            _locations = locations;
        }

        public bool Enabled { get; set; }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!Enabled) return;
            if (!TorchMonitorConfig.Instance.GeoLocationEnabled) return;
            if (intervalsSinceStart % 20 != 0) return;

            var continents = new Dictionary<string, int>();
            var countries = new Dictionary<string, int>();
            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) return;

                var steamId = onlinePlayer.SteamId();
                if (_locations.TryGetLocation(steamId, out var ipLocation))
                {
                    // null when localhost
                    continents.Increment(ipLocation.ContinentName ?? "<unknown>");
                    countries.Increment(ipLocation.CountryName ?? "<unknown>");
                }
                else
                {
                    // update `_ipLocations` (for next interval)
                    _locations.Load(steamId).Forget(Log);
                }
            }

            foreach (var (continentName, count) in continents)
            {
                Log.Trace($"writing continent data: '{continentName}'");

                TorchInfluxDbWriter
                    .Measurement("players_continents")
                    .Tag("continent_name", continentName)
                    .Field("online_player_count", count)
                    .Write();
            }

            foreach (var (countryName, count) in countries)
            {
                Log.Trace($"writing country data: '{countryName}'");

                TorchInfluxDbWriter
                    .Measurement("players_countries")
                    .Tag("country_name", countryName)
                    .Field("online_player_count", count)
                    .Write();
            }
        }
    }
}