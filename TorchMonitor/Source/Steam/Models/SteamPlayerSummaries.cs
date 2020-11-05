using Newtonsoft.Json;

namespace TorchMonitor.Steam.Models
{
    public sealed class SteamPlayerSummaries
    {
        [JsonProperty("players")]
        public SteamPlayerSummary[] List { get; private set; }
    }
}