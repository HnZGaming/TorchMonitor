using System.Xml.Serialization;
using Intervals;
using Ipstack;
using Torch;
using Torch.Views;
using TorchMonitor.Monitors;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig :
        ViewModel,
        IpstackEndpoints.IConfig,
        GeoLocationMonitor.IConfig,
        IntervalRunner.IConfig,
        IMonitorGeneralConfig
    {
        string _ipstackApiKey = "apikey";
        bool _enableIntervalLog;
        int _firstIgnoredSeconds = 120;

        [XmlElement("Ipstack.ApiKey")]
        [Display(Name = "Ipstack.ApiKey")]
        public string ApiKey
        {
            get => _ipstackApiKey;
            set => SetProperty(ref _ipstackApiKey, value);
        }

        [XmlElement("IntervalRunner.EnableLog")]
        [Display(Name = "Enable Interval Logging")]
        public bool EnableLog
        {
            get => _enableIntervalLog;
            set => SetProperty(ref _enableIntervalLog, value);
        }

        [XmlElement("FirstIgnoredSeconds")]
        [Display(Name = "First Ignored Seconds")]
        public int FirstIgnoredSeconds
        {
            get => _firstIgnoredSeconds;
            set => SetProperty(ref _firstIgnoredSeconds, value);
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);

        // ReSharper disable once RedundantAssignment
        void SetProperty<T>(ref T property, T value)
        {
            property = value;
            OnPropertyChanged();
        }
    }
}