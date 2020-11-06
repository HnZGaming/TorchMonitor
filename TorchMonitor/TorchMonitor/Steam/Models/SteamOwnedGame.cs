using Newtonsoft.Json;

namespace TorchMonitor.Steam.Models
{
    public sealed class SteamOwnedGame
    {
        [JsonProperty("appid")]
        public int AppId { get; private set; }

        [JsonProperty("playtime_forever")]
        public ulong TotalPlaytimeMinutes { get; private set; }

        [JsonProperty("playtime_2weeks")]
        public ulong RecentPlaytimeMinutes { get; private set; }
    }
}