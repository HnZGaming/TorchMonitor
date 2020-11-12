using System.Collections.Generic;
using System.Linq;
using InfluxDb;
using Intervals;
using Sandbox.Game.World;
using TorchUtils;

namespace TorchMonitor.Monitors
{
    public sealed class OnlinePlayersMonitor : IIntervalListener
    {
        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart % 10 != 0) return;

            var onlinePlayers = MySession.Static.Players.GetOnlinePlayers().ToArray();

            InfluxDbPointFactory
                .Measurement("server")
                .Field("players", onlinePlayers.Length)
                .Write();

            var factionList = MySession.Static.Factions.Factions.Values;
            var factions = new Dictionary<string, int>();
            foreach (var onlinePlayer in onlinePlayers)
            {
                if (onlinePlayer == null) continue;

                var steamId = onlinePlayer.SteamId();
                var playerId = onlinePlayer.PlayerId();

                var faction = factionList.FirstOrDefault(f => f.Members.ContainsKey(playerId));
                var factionTag = faction?.Tag ?? "<single>";
                factions.Increment(factionTag);

                InfluxDbPointFactory
                    .Measurement("players_players")
                    .Tag("steam_id", $"{steamId}")
                    .Tag("player_name", onlinePlayer.DisplayName)
                    .Tag("faction_tag", factionTag)
                    .Field("is_online", 1)
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
        }
    }
}