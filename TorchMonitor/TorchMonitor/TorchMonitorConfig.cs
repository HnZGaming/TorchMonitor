using System.Xml.Serialization;
using Intervals;
using Ipstack;
using Torch;
using Torch.Views;
using TorchMonitor.Monitors;
using TorchMonitor.ProfilerMonitors;
using TorchMonitor.Utils;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig :
        ViewModel,
        IpstackEndpoints.IConfig,
        GeoLocationMonitor.IConfig,
        IntervalRunner.IConfig,
        IMonitorGeneralConfig,
        GridProfilerMonitor.IConfig
    {
        bool _enabled = true;
        string _ipstackApiKey = "apikey";
        int _firstIgnoredSeconds = 120;
        bool _gridProfilerDetailOutput;

        [XmlElement("Enabled")]
        [Display(Order = 0, Name = "Enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [XmlElement("FirstIgnoredSeconds")]
        [Display(Order = 2, Name = "First Ignored Seconds")]
        public int FirstIgnoredSeconds
        {
            get => _firstIgnoredSeconds;
            set => SetValue(ref _firstIgnoredSeconds, value);
        }

        [XmlElement("Ipstack.ApiKey")]
        [Display(Order = 3, Name = "Ipstack.ApiKey")]
        public string ApiKey
        {
            get => _ipstackApiKey;
            set => SetValue(ref _ipstackApiKey, value);
        }

        [XmlElement("GridProfilerMonitor.DetailOutput")]
        [Display(Order = 4, Name = "Grid Profiler Detail Output")]
        public bool DetailOutput
        {
            get => _gridProfilerDetailOutput;
            set => SetValue(ref _gridProfilerDetailOutput, value);
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);
    }
}