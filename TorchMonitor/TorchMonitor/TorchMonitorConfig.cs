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
        GridProfilerMonitor.IConfig, 
        SessionComponentsProfilerMonitor.IConfig
    {
        const string OpGroupName = "Operation";
        const string OutputGroupName = "Output";

        bool _enabled = true;
        string _ipstackApiKey = "apikey";
        int _firstIgnoredSeconds = 120;
        bool _gridProfilerDetailOutput;
        bool _resolveNameConflict = true;
        bool _monitorSessionComponentNamespace;

        [XmlElement]
        [Display(Order = 0, Name = "Enabled", GroupName = OpGroupName)]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [XmlElement]
        [Display(
            Order = 2, Name = "First ignored seconds", GroupName = OpGroupName,
            Description = "Skip writing for the first N seconds of the session.")]
        public int FirstIgnoredSeconds
        {
            get => _firstIgnoredSeconds;
            set => SetValue(ref _firstIgnoredSeconds, value);
        }

        [XmlElement("Ipstack.ApiKey")]
        [Display(Order = 3, Name = "Ipstack.ApiKey", GroupName = OpGroupName)]
        public string ApiKey
        {
            get => _ipstackApiKey;
            set => SetValue(ref _ipstackApiKey, value);
        }

        [XmlElement("GridProfilerMonitor.DetailOutput")]
        [Display(
            Name = "Grid owners", GroupName = OutputGroupName,
            Description = "Show the name of grid owners.")]
        public bool ShowOwnerName
        {
            get => _gridProfilerDetailOutput;
            set => SetValue(ref _gridProfilerDetailOutput, value);
        }

        [XmlElement("GridProfilerMonitor.ResolveNameConflict")]
        [Display(Name = "Resolve Grid Name Conflict", GroupName = OutputGroupName,
            Description = "Show entity ID if multiple grids share the same name.")]
        public bool ResolveNameConflict
        {
            get => _resolveNameConflict;
            set => SetValue(ref _resolveNameConflict, value);
        }

        bool GeoLocationMonitor.IConfig.Enabled => !string.IsNullOrEmpty(ApiKey);

        [XmlElement("SessionComponentsProfilerMonitor.MonitorNamespace")]
        [Display(Name = "Monitor namespace of session components", GroupName = OutputGroupName,
            Description = "Show type namespace of session components.")]
        public bool MonitorSessionComponentNamespace
        {
            get => _monitorSessionComponentNamespace;
            set => SetValue(ref _monitorSessionComponentNamespace, value);
        }
    }
}