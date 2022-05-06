using System;
using System.Xml.Serialization;
using NLog;
using Torch;
using Utils.Torch;
using VRageMath;

namespace TorchMonitor
{
    public sealed class TorchMonitorConfig : ViewModel, FileLoggingConfigurator.IConfig
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        public const string DefaultLogPath = "Logs/TorchMonitor-${shortdate}.log";

        public static TorchMonitorConfig Instance { get; set; }

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
        bool _suppressWpfOutput;
        bool _enableLoggingTrace;
        bool _enableLoggingDebug;
        string _logFilePath = DefaultLogPath;
        bool _ignoreAnimals;

        [XmlElement]
        public bool Enabled
        {
            get => _enabled;
            set => SetValue(ref _enabled, value);
        }

        [XmlElement]
        public int FirstIgnoredSeconds
        {
            get => _firstIgnoredSeconds;
            set => SetValue(ref _firstIgnoredSeconds, value);
        }

        [XmlElement]
        public bool IgnoreAnimals
        {
            get => _ignoreAnimals;
            set => SetValue(ref _ignoreAnimals, value);
        }

        [XmlElement("Ipstack.ApiKey")]
        public string IpStackApiKey
        {
            get => _ipstackApiKey;
            set => SetValue(ref _ipstackApiKey, value);
        }

        [XmlElement("GridProfilerMonitor.DetailOutput")]
        public bool ShowOwnerName
        {
            get => _gridProfilerDetailOutput;
            set => SetValue(ref _gridProfilerDetailOutput, value);
        }

        [XmlElement("GridProfilerMonitor.ResolveNameConflict")]
        public bool ResolveNameConflict
        {
            get => _resolveNameConflict;
            set => SetValue(ref _resolveNameConflict, value);
        }

        public bool GeoLocationEnabled => !string.IsNullOrEmpty(IpStackApiKey);

        [XmlElement("SessionComponentsProfilerMonitor.MonitorNamespace")]
        public bool MonitorSessionComponentNamespace
        {
            get => _monitorSessionComponentNamespace;
            set => SetValue(ref _monitorSessionComponentNamespace, value);
        }

        [XmlElement]
        public bool PhysicsEnabled
        {
            get => _physicsEnabled;
            set => SetValue(ref _physicsEnabled, value);
        }

        [XmlElement]
        public int PhysicsInterval
        {
            get => _physicsInterval;
            set => SetValue(ref _physicsInterval, value);
        }

        [XmlElement]
        public int PhysicsFrameCount
        {
            get => _physicsFrameCount;
            set => SetValue(ref _physicsFrameCount, value);
        }

        [XmlElement]
        public int PhysicsMaxClusterCount
        {
            get => _physicsMaxClusterCount;
            set => SetValue(ref _physicsMaxClusterCount, value);
        }

        [XmlElement]
        public bool EnableNexusFeature
        {
            get => _enableNexusFeature;
            set => SetValue(ref _enableNexusFeature, value);
        }

        [XmlElement]
        public string NexusPrefix
        {
            get => _nexusPrefix;
            set
            {
                SetValue(ref _nexusPrefix, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        [XmlElement]
        public double NexusOriginPositionX
        {
            get => _nexusOriginPositionX;
            set
            {
                SetValue(ref _nexusOriginPositionX, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        [XmlElement]
        public double NexusOriginPositionY
        {
            get => _nexusOriginPositionY;
            set
            {
                SetValue(ref _nexusOriginPositionY, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        [XmlElement]
        public double NexusOriginPositionZ
        {
            get => _nexusOriginPositionZ;
            set
            {
                SetValue(ref _nexusOriginPositionZ, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        public Vector3D NexusOriginPosition => new(
            _nexusOriginPositionX,
            _nexusOriginPositionY,
            _nexusOriginPositionZ);

        [XmlElement]
        public double NexusSectorDiameter
        {
            get => _nexusSectorDiameter;
            set
            {
                SetValue(ref _nexusSectorDiameter, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        [XmlElement]
        public int NexusSegmentationCount
        {
            get => _nexusSegmentationCount;
            set
            {
                SetValue(ref _nexusSegmentationCount, value);
                OnPropertyChanged(nameof(NexusPreview));
            }
        }

        public string NexusPreview => $"{Math.Pow(NexusSegmentationCount, 3)} segments; tags e.g.: \"nexus_{NexusPrefix}0_0_0\"";

        [XmlElement]
        public bool SuppressWpfOutput
        {
            get => _suppressWpfOutput;
            set => SetValue(ref _suppressWpfOutput, value);
        }

        [XmlElement]
        public bool EnableLoggingTrace
        {
            get => _enableLoggingTrace;
            set => SetValue(ref _enableLoggingTrace, value);
        }

        [XmlElement]
        public bool EnableLoggingDebug
        {
            get => _enableLoggingDebug;
            set => SetValue(ref _enableLoggingDebug, value);
        }

        [XmlElement]
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetValue(ref _logFilePath, value);
        }
    }
}