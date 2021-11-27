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
        ITorchMonitorGeneralConfig,
        GridProfilerMonitor.IConfig,
        SessionComponentsProfilerMonitor.IConfig,
        PhysicsProfilerMonitor.IConfig
    {
        const string OpGroupName = "Operation";
        const string OutputGroupName = "Output";
        const string PhysicsGroupName = "Physics";

        bool _enabled = true;
        string _ipstackApiKey = "apikey";
        int _firstIgnoredSeconds = 120;
        bool _gridProfilerDetailOutput;
        bool _resolveNameConflict = true;
        bool _monitorSessionComponentNamespace;
        int _physicsInterval = 60;
        int _physicsFrameCount = 10;
        int _physicsMaxClusterCount = 5;
        bool _physicsEnabled = true;

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

        [XmlElement]
        [Display(Name = "Enabled", GroupName = PhysicsGroupName, Description = "Profile physics", Order = 0)]
        public bool PhysicsEnabled
        {
            get => _physicsEnabled;
            set => SetValue(ref _physicsEnabled, value);
        }

        [XmlElement]
        [Display(Name = "Interval (seconds)", GroupName = PhysicsGroupName, Description = "Profile physics every N seconds.", Order = 1)]
        public int PhysicsInterval
        {
            get => _physicsInterval;
            set => SetValue(ref _physicsInterval, value);
        }

        [XmlElement]
        [Display(Name = "Frame count", GroupName = PhysicsGroupName, Description = "Profile physics N frames at once.", Order = 2)]
        public int PhysicsFrameCount
        {
            get => _physicsFrameCount;
            set => SetValue(ref _physicsFrameCount, value);
        }

        [XmlElement]
        [Display(Name = "Max entity count", GroupName = PhysicsGroupName, Description = "Submit N top clusters every interval.", Order = 3)]
        public int PhysicsMaxClusterCount
        {
            get => _physicsMaxClusterCount;
            set => SetValue(ref _physicsMaxClusterCount, value);
        }
    }
}