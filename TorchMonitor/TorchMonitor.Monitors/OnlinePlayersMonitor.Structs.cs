using Newtonsoft.Json;
using Utils.General;

namespace TorchMonitor.Monitors
{
    public sealed partial class OnlinePlayersMonitor
    {
        class PlayerOnlineTime
        {
            [JsonProperty("steam_id"), StupidJsonDbKey]
            public string SteamId { get; set; }

            [JsonProperty("online_time")]
            public double OnlineTime { get; set; }
        }
    }
}