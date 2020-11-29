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
        bool _enabled = true;
        string _ipstackApiKey = "apikey";
        bool _enableIntervalLog;
        int _firstIgnoredSeconds = 120;
        bool _resetLocalDatabaseOnNextStart;

        [XmlElement("Enabled")]
        [Display(Name = "Enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

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

        [XmlElement("ResetLocalDatabaseOnNextStart")]
        [Display(Name = "Reset Local Database On Next Start")]
        public bool ResetLocalDatabaseOnNextStart
        {
            get => _resetLocalDatabaseOnNextStart;
            set => SetProperty(ref _resetLocalDatabaseOnNextStart, value);
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);

        void SetProperty<T>(ref T property, T value)
        {
            if (!property.Equals(value))
            {
                property = value;
                OnPropertyChanged();
            }
        }
    }
}