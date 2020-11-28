using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using InfluxDb;
using Intervals;
using Ipstack;
using NLog;
using Sandbox.Game.World;
using TorchMonitor.Reflections;
using Utils.General;
using Utils.Torch;
using VRage.GameServices;

namespace TorchMonitor.Monitors
{
    public sealed class GeoLocationMonitor : IIntervalListener
    {
        public interface IConfig
        {
            bool Enabled { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IpstackEndpoints _ipstackEndpoints;
        readonly IConfig _config;
        readonly ConcurrentDictionary<ulong, IpstackLocation> _ipLocations;

        public GeoLocationMonitor(IpstackEndpoints ipstackEndpoints, IConfig config)
        {
            _ipstackEndpoints = ipstackEndpoints;
            _config = config;
            _ipLocations = new ConcurrentDictionary<ulong, IpstackLocation>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (!_config.Enabled) return;
            if (intervalsSinceStart % 20 != 0) return;

            var continents = new Dictionary<string, int>();
            var countries = new Dictionary<string, int>();
            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) return;

                var steamId = onlinePlayer.SteamId();
                if (_ipLocations.TryGetValue(steamId, out var ipLocation))
                {
                    // null when localhost
                    continents.Increment(ipLocation.ContinentName ?? "<unknown>");
                    countries.Increment(ipLocation.CountryName ?? "<unknown>");
                }
                else
                {
                    // update `_ipLocations` (for next interval)
                    LoadIpLocation(steamId).Forget(Log);
                }
            }

            foreach (var (continentName, count) in continents)
            {
                Log.Trace($"writing continent data: '{continentName}'");

                InfluxDbPointFactory
                    .Measurement("players_continents")
                    .Tag("continent_name", continentName)
                    .Field("online_player_count", count)
                    .Write();
            }

            foreach (var (countryName, count) in countries)
            {
                Log.Trace($"writing country data: '{countryName}'");

                InfluxDbPointFactory
                    .Measurement("players_countries")
                    .Tag("country_name", countryName)
                    .Field("online_player_count", count)
                    .Write();
            }
        }

        async Task LoadIpLocation(ulong steamId)
        {
            // Get the public IP of the Steam player
            var state = new MyP2PSessionState();
            var networking = MySteamGameService_Networking.Value;
            networking.Peer2Peer.GetSessionState(steamId, ref state);
            var ip = BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray();
            var ipAddress = new IPAddress(ip).ToString();

            // Get the location
            var location = await _ipstackEndpoints.GetLocationOrNullAsync(ipAddress);
            _ipLocations[steamId] = location ?? new IpstackLocation();
        }
    }
}