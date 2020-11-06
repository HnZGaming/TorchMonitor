using Newtonsoft.Json;

namespace TorchMonitor.Steam.Models
{
    public sealed class SteamOwnedGames
    {
        [JsonProperty("game_count")]
        public int Count { get; private set; }

        [JsonProperty("games")]
        public SteamOwnedGame[] List { get; private set; }
    }
}