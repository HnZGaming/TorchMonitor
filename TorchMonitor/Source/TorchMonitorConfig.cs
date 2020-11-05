using System.Xml.Serialization;
using TorchMonitor.Business.Monitors;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig : FactionConcealmentMonitor.IConfig
    {
        [XmlElement("FactionConcealmentMonitor.CollectIntervalSecs")]
        public int CollectIntervalSecs { get; set; } = 20;

        [XmlElement("FactionConcealmentMonitor.WriteIntervalSecs")]
        public int WriteIntervalSecs { get; set; } = 10;

        [XmlElement("FactionConcealmentMonitor.FactionTag")]
        public string FactionTag { get; set; } = "MME";

        [XmlElement("SteamApiKey")]
        public string SteamApiKey { get; set; } = "";

        [XmlElement("IpstackApiKey")]
        public string IpstackApiKey { get; set; } = "";
    }
}