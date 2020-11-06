﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch;
using TorchDatabaseIntegration.InfluxDB;
using TorchMonitor.Ipstack;
using TorchMonitor.Steam;
using TorchMonitor.Steam.Models;
using TorchUtils;
using VRage.GameServices;

namespace TorchMonitor.Business.Monitors
{
    public sealed partial class OnlinePlayersMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly SteamApiEndpoints _steamApiEndpoints;
        readonly IpstackEndpoints _ipstackEndpoints;
        readonly ConcurrentDictionary<ulong, SteamOwnedGame> _steamGameStates;
        readonly ConcurrentDictionary<ulong, IpstackLocation> _ipLocations;

        public OnlinePlayersMonitor(SteamApiEndpoints steamApiEndpoints, IpstackEndpoints ipstackEndpoints)
        {
            _steamApiEndpoints = steamApiEndpoints;
            _ipstackEndpoints = ipstackEndpoints;
            _steamGameStates = new ConcurrentDictionary<ulong, SteamOwnedGame>();
            _ipLocations = new ConcurrentDictionary<ulong, IpstackLocation>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            // collect data

            var factionList = MySession.Static.Factions.Factions.Values;
            var onlinePlayers = new List<PlayerInfo>();
            var factions = new Dictionary<string, int>();
            var continents = new Dictionary<string, int>();
            var countries = new Dictionary<string, int>();
            foreach (var anyPlayer in MySession.Static.Players.GetAllIdentities())
            {
                if (!TryGetOnlineTime(anyPlayer, out var activeTime)) continue;

                var playerFaction = factionList.FirstOrDefault(f => f.Members.ContainsKey(anyPlayer.IdentityId));
                var playerFactionTag = playerFaction?.Tag ?? "<single>";
                factions.Increment(playerFactionTag);

                var steamId = anyPlayer.Character.ControlSteamId;

                var playerInfo = new PlayerInfo
                {
                    SteamId = steamId,
                    Name = anyPlayer.DisplayName,
                    FactionTag = playerFactionTag,
                    OnlineTime = activeTime,
                };

                if (_steamGameStates.TryGetValue(steamId, out var steamGameState))
                {
                    var totalPlayTimeMinutes = (double) steamGameState.TotalPlaytimeMinutes;
                    totalPlayTimeMinutes += activeTime.TotalMinutes;
                    playerInfo.TotalGamePlayTime = TimeSpan.FromMinutes(totalPlayTimeMinutes);
                }
                else
                {
                    LoadGameState(steamId).Forget(Log);
                }

                if (_ipLocations.TryGetValue(steamId, out var ipLocation))
                {
                    // null when localhost
                    continents.Increment(ipLocation.ContinentName ?? "<unknown>");
                    countries.Increment(ipLocation.CountryName ?? "<unknown>");
                }
                else
                {
                    LoadIpLocation(steamId).Forget(Log);
                }

                onlinePlayers.Add(playerInfo);
            }

            // write data

            InfluxDbPointFactory
                .Measurement("server")
                .Field("players", onlinePlayers.Count)
                .Write();

            foreach (var onlinePlayer in onlinePlayers)
            {
                InfluxDbPointFactory
                    .Measurement("players_players")
                    .Tag("steamId", $"{onlinePlayer.SteamId}")
                    .Tag("player_name", onlinePlayer.Name)
                    .Tag("faction_tag", onlinePlayer.FactionTag)
                    .Field("active_time", onlinePlayer.OnlineTime.TotalMinutes)
                    .Field("total_active_time", onlinePlayer.TotalGamePlayTime.TotalHours)
                    .Write();
            }

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                InfluxDbPointFactory
                    .Measurement("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount)
                    .Write();
            }

            foreach (var (continentName, count) in continents)
            {
                InfluxDbPointFactory
                    .Measurement("players_continents")
                    .Tag("continent_name", continentName)
                    .Field("online_player_count", count)
                    .Write();
            }

            foreach (var (countryName, count) in countries)
            {
                InfluxDbPointFactory
                    .Measurement("players_countries")
                    .Tag("country_name", countryName)
                    .Field("online_player_count", count)
                    .Write();
            }
        }

        async Task LoadGameState(ulong steamId)
        {
            var states = await _steamApiEndpoints.GetOwnedGames(steamId);
            var state = states?.First(s => s.AppId == 244850) ?? new SteamOwnedGame();
            _steamGameStates[steamId] = state;
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