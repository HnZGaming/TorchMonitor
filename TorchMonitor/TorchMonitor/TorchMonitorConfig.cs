using System;
using System.Xml.Serialization;
using Intervals;
using Ipstack;
using Torch;
using Torch.Views;
using TorchMonitor.Monitors;
using TorchMonitor.ProfilerMonitors;
using VRageMath;

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
        PhysicsProfilerMonitor.IConfig,
        TorchMonitorNexus.IConfig
    {
        const string OpGroupName = "Operation";
        const string OutputGroupName = "Output";
        const string PhysicsGroupName = "Physics";
        const string NexusGroupName = "Nexus";

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

        bool _enableNexusFeature;
        double _nexusOriginPositionX;
        double _nexusOriginPositionY;
        double _nexusOriginPositionZ;
        double _nexusSectorDiameter = 1000000;
        int _nexusSegmentationCount = 3;
        string _nexusPrefix = "foo_";

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

        [XmlElement]
        [Display(Name = "Enable Nexus features", GroupName = NexusGroupName, Order = 0)]
        public bool EnableNexusFeature
        {
            get => _enableNexusFeature;
            set
            {
                SetValue(ref _enableNexusFeature, value);
                RefreshModel();
            }
        }

        [XmlElement]
        [Display(Name = "Prefix", GroupName = NexusGroupName, Order = 1)]
        public string NexusPrefix
        {
            get => _nexusPrefix;
            set
            {
                SetValue(ref _nexusPrefix, value);
                RefreshModel();
            }
        }

        [XmlElement]
        [Display(Name = "Origin position (x)", GroupName = NexusGroupName, Order = 2)]
        public double NexusOriginPositionX
        {
            get => _nexusOriginPositionX;
            set => SetValue(ref _nexusOriginPositionX, value);
        }

        [XmlElement]
        [Display(Name = "Origin position (y)", GroupName = NexusGroupName, Order = 3)]
        public double NexusOriginPositionY
        {
            get => _nexusOriginPositionY;
            set => SetValue(ref _nexusOriginPositionY, value);
        }

        [XmlElement]
        [Display(Name = "Origin position (z)", GroupName = NexusGroupName, Order = 4)]
        public double NexusOriginPositionZ
        {
            get => _nexusOriginPositionZ;
            set => SetValue(ref _nexusOriginPositionZ, value);
        }

        public Vector3D NexusOriginPosition => new Vector3D(
            _nexusOriginPositionX,
            _nexusOriginPositionY,
            _nexusOriginPositionZ);

        [XmlElement]
        [Display(Name = "Sector diameter", GroupName = NexusGroupName, Order = 5)]
        public double NexusSectorDiameter
        {
            get => _nexusSectorDiameter;
            set => SetValue(ref _nexusSectorDiameter, value);
        }

        [XmlElement]
        [Display(Name = "Segmentation per dimension", GroupName = NexusGroupName, Order = 6)]
        public int NexusSegmentationCount
        {
            get => _nexusSegmentationCount;
            set
            {
                SetValue(ref _nexusSegmentationCount, value);
                RefreshModel();
            }
        }

        [Display(Name = "Type !tm nexus", GroupName = NexusGroupName, Order = 7)]
        public string Foo => EnableNexusFeature ? $"{Math.Pow(NexusSegmentationCount, 3)} segments; measurement name e.g.: \"nexus_{NexusPrefix}0_0_0\"" : "disabled";
    }
}