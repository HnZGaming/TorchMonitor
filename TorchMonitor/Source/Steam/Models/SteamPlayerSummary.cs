using Newtonsoft.Json;

namespace TorchMonitor.Steam.Models
{
    public sealed class SteamPlayerSummary
    {
        [JsonProperty("steamid")]
        public string SteamId { get; private set; }

        [JsonProperty("communityvisibilitystate")]
        public SteamCommunityVisibility CommunityVisibility { get; private set; }

        [JsonProperty("personaname")]
        public string PersonaName { get; private set; }

        [JsonProperty("profileurl")]
        public string ProfileUrl { get; private set; }

        [JsonProperty("lastlogoff")]
        public ulong LastLogOff { get; private set; }

        [JsonProperty("realname")]
        public string RealName { get; private set; }

        [JsonProperty("timecreated")]
        public ulong TimeCreated { get; private set; }

        [JsonProperty("loccountrycode")]
        public string CountryCode { get; private set; }

        [JsonProperty("locstatecode")]
        public string StateCode { get; private set; }

        [JsonProperty("loccityid")]
        public int CityId { get; private set; }
    }
}