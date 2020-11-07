using System;

namespace TorchMonitor.Business.Monitors
{
    public sealed partial class OnlinePlayersMonitor
    {
        struct PlayerInfo
        {
            public ulong SteamId { get; set; }
            public string Name { get; set; }
            public string FactionTag { get; set; }
            public TimeSpan OnlineTime { get; set; }
        }
    }
}