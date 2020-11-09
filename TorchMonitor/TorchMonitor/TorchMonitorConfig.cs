using System.Xml.Serialization;
using TorchMonitor.Business.Monitors;
using TorchMonitor.Ipstack;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig : FactionGridMonitor.IConfig
    {
        [XmlElement("FactionGridMonitor.CollectIntervalSecs")]
        public int CollectIntervalSecs { get; set; } = 20;

        [XmlElement("FactionGridMonitor.WriteIntervalSecs")]
        public int WriteIntervalSecs { get; set; } = 10;

        [XmlElement("FactionGridMonitor.FactionTag")]
        public string FactionTag { get; set; } = "MME";

        [XmlElement("IpstackApiKey")]
        public string IpstackApiKey { get; set; } = IpstackEndpoints.ApiKeyPlaceholder;
    }
}