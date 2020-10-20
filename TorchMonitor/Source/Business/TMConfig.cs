using System.Xml.Serialization;

namespace TorchMonitor.Business
{
    public sealed class TMConfig : IFactionConcealmentMonitorConfig
    {
        [XmlElement("FactionConcealmentMonitor.CollectIntervalSecs")]
        public int CollectIntervalSecs { get; set; } = 20;

        [XmlElement("FactionConcealmentMonitor.WriteIntervalSecs")]
        public int WriteIntervalSecs { get; set; } = 10;

        [XmlElement("FactionConcealmentMonitor.FactionTag")]
        public string FactionTag { get; set; } = "MME";
    }
}