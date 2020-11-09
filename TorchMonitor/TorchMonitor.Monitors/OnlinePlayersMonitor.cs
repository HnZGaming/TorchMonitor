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
using Torch;
using TorchUtils;
using VRage.GameServices;

namespace TorchMonitor.Monitors
{
    public sealed partial class OnlinePlayersMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IpstackEndpoints _ipstackEndpoints;
        readonly ConcurrentDictionary<ulong, IpstackLocation> _ipLocations;

        public OnlinePlayersMonitor(IpstackEndpoints ipstackEndpoints)
        {
            _ipstackEndpoints = ipstackEndpoints;
            _ipLocations = new ConcurrentDictionary<ulong, IpstackLocation>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            Log.Trace("collecting data");

            var factionList = MySession.Static.Factions.Factions.Values;
            var playerInfos = new List<PlayerInfo>();
            var factions = new Dictionary<string, int>();
            var continents = new Dictionary<string, int>();
            var countries = new Dictionary<string, int>();
            foreach (var anyPlayer in MySession.Static.Players.GetAllIdentities())
            {
                if (anyPlayer == null) continue;
                if (!TryGetOnlineTime(anyPlayer, out var activeTime)) continue;

                if (anyPlayer.Character == null) continue;
                var steamId = anyPlayer.Character.ControlSteamId;

                Log.Trace($"collecting online player: '{anyPlayer.DisplayName}' ({steamId})");

                var playerFaction = factionList.FirstOrDefault(f => f.Members.ContainsKey(anyPlayer.IdentityId));
                var playerFactionTag = playerFaction?.Tag ?? "<single>";
                factions.Increment(playerFactionTag);

                var playerInfo = new PlayerInfo
                {
                    SteamId = steamId,
                    Name = anyPlayer.DisplayName,
                    FactionTag = playerFactionTag,
                    OnlineTime = activeTime,
                };

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

                playerInfos.Add(playerInfo);
            }

            Log.Trace("writing data");

            InfluxDbPointFactory
                .Measurement("server")
                .Field("players", playerInfos.Count)
                .Write();

            foreach (var playerInfo in playerInfos)
            {
                Log.Trace($"writing player data: '{playerInfo.Name}' ({playerInfo.SteamId})");

                InfluxDbPointFactory
                    .Measurement("players_players")
                    .Tag("steam_id", $"{playerInfo.SteamId}")
                    .Tag("player_name", playerInfo.Name)
                    .Tag("faction_tag", playerInfo.FactionTag)
                    .Field("active_time", playerInfo.OnlineTime.TotalMinutes)
                    .Write();
            }

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                Log.Trace($"writing faction data: '{factionTag}'");

                InfluxDbPointFactory
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
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
            
            Log.Trace("interval done");
        }

        async Task LoadIpLocation(ulong steamId)
        {
            var state = new MyP2PSessionState();
            MySteamServiceWrapper.Static.Peer2Peer.GetSessionState(steamId, ref state);
            var ip = BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray();
            var ipAddress = new IPAddress(ip).ToString();
            var location = await _ipstackEndpoints.Query(ipAddress);
            _ipLocations[steamId] = location ?? new IpstackLocation();
        }

        static bool TryGetOnlineTime(MyIdentity player, out TimeSpan activeTime)
        {
            activeTime = default;

            var isPlayerOnline = MySession.Static.Players.IsPlayerOnline(player.IdentityId);
            if (!isPlayerOnline) return false;

            activeTime = DateTime.Now - player.LastLoginTime;
            return true;
        }
    }
}