using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client.Writes;
using NLog;
using Sandbox.Game.World;
using Torch.Server.InfluxDb;
using TorchMonitor.Steam;
using TorchMonitor.Steam.Models;
using TorchMonitor.Utils;

namespace TorchMonitor.Business.Monitors
{
    public sealed partial class OnlinePlayersMonitor : IIntervalListener
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly InfluxDbClient _dbClient;
        readonly SteamApiEndpoints _steamApiEndpoints;
        readonly ConcurrentDictionary<ulong, SteamOwnedGame> _steamGameStates;

        public OnlinePlayersMonitor(InfluxDbClient dbClient, SteamApiEndpoints steamApiEndpoints)
        {
            _dbClient = dbClient;
            _steamApiEndpoints = steamApiEndpoints;
            _steamGameStates = new ConcurrentDictionary<ulong, SteamOwnedGame>();
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            // collect data

            var factionList = MySession.Static.Factions.Factions.Values;
            var onlinePlayers = new List<PlayerInfo>();
            var factions = new Dictionary<string, int>();
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

                onlinePlayers.Add(playerInfo);
            }

            // write data

            var points = new List<PointData>();

            {
                var point = _dbClient
                    .MakePointIn("server")
                    .Field("players", onlinePlayers.Count);

                _dbClient.WritePoints(point);
            }

            foreach (var onlinePlayer in onlinePlayers)
            {
                var point = _dbClient
                    .MakePointIn("players_players")
                    .Tag("steamId", $"{onlinePlayer.SteamId}")
                    .Tag("player_name", onlinePlayer.Name)
                    .Tag("faction_tag", onlinePlayer.FactionTag)
                    .Field("active_time", onlinePlayer.OnlineTime.TotalMinutes)
                    .Field("total_active_time", onlinePlayer.TotalGamePlayTime.TotalMinutes);

                points.Add(point);
            }

            foreach (var (factionTag, onlineMemberCount) in factions)
            {
                var point = _dbClient
                    .MakePointIn("players_factions")
                    .Tag("faction_tag", factionTag)
                    .Field("online_member_count", onlineMemberCount);

                points.Add(point);
            }

            _dbClient.WritePoints(points.ToArray());
        }

        async Task LoadGameState(ulong steamId)
        {
            var states = await _steamApiEndpoints.GetOwnedGames(steamId);
            var state = states.First(s => s.AppId == 244850);
            _steamGameStates[steamId] = state;
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