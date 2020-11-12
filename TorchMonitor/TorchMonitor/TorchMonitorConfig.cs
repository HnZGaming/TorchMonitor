using System.Xml.Serialization;
using Ipstack;
using Torch;
using Torch.Views;
using TorchMonitor.Monitors;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig :
        ViewModel,
        FactionGridMonitor.IConfig,
        IpstackEndpoints.IConfig,
        GeoLocationMonitor.IConfig
    {
        int _collectIntervalSecs = 20;
        int _writeIntervalSecs = 10;
        string _factionTag = "MME";
        string _ipstackApiKey = "apikey";

        [XmlElement("FactionGridMonitor.CollectIntervalSecs")]
        [Display(Name = "FactionGridMonitor.CollectIntervalSecs")]
        public int CollectIntervalSecs
        {
            get => _collectIntervalSecs;
            set => SetProperty(ref _collectIntervalSecs, value);
        }

        [XmlElement("FactionGridMonitor.WriteIntervalSecs")]
        [Display(Name = "FactionGridMonitor.WriteIntervalSecs")]
        public int WriteIntervalSecs
        {
            get => _writeIntervalSecs;
            set => SetProperty(ref _writeIntervalSecs, value);
        }

        [XmlElement("FactionGridMonitor.FactionTag")]
        [Display(Name = "FactionGridMonitor.FactionTag")]
        public string FactionTag
        {
            get => _factionTag;
            set => SetProperty(ref _factionTag, value);
        }

        [XmlElement("Ipstack.ApiKey")]
        [Display(Name = "Ipstack.ApiKey")]
        public string ApiKey
        {
            get => _ipstackApiKey;
            set => SetProperty(ref _ipstackApiKey, value);
        }

        // ReSharper disable once RedundantAssignment
        void SetProperty<T>(ref T property, T value)
        {
            property = value;
            OnPropertyChanged();
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);
    }
}