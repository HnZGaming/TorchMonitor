using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchMonitor.ProfilerMonitors;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchMonitorConfig> _config;
        TorchMonitorControl _userControl;

        CancellationTokenSource _canceller;
        IntervalRunner _intervalRunner;
        IpstackEndpoints _ipstackEndpoints;
        FileLoggingConfigurator _fileLogger;

        UserControl IWpfPlugin.GetControl()
        {
            return _userControl ??= new TorchMonitorControl(this);
        }

        public TorchMonitorNexus Nexus { get; private set; }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _canceller = new CancellationTokenSource();

            _fileLogger = new FileLoggingConfigurator(
                nameof(TorchMonitor),
                new[]
                {
                    $"{nameof(TorchMonitor)}.*",
                    $"{nameof(Intervals)}.*",
                },
                TorchMonitorConfig.DefaultLogPath);
            _fileLogger.Initialize();

            ReloadConfig();

            _ipstackEndpoints = new IpstackEndpoints();

            var localDbFilePath = this.MakeFilePath($"{nameof(TorchMonitor)}.json");
            var playerOnlineTimeDb = new PlayerOnlineTimeDb(localDbFilePath);
            playerOnlineTimeDb.Read();

            var gridNameConflictSolver = new NameConflictSolver<long>();
            var playerNameConflictSolver = new NameConflictSolver<ulong>();
            Nexus = new TorchMonitorNexus();

            _intervalRunner = new IntervalRunner(1);
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(),
                new GridMonitor(),
                //new FloatingObjectsMonitor(Config),
                new RamUsageMonitor(),
                //new VoxelMonitor(),
                new PingMonitor(),
                new OnlinePlayersMonitor(playerNameConflictSolver, playerOnlineTimeDb, Nexus),
                new GeoLocationMonitor(_ipstackEndpoints),
                new BlockTypeProfilerMonitor(),
                new EntityTypeProfilerMonitor(),
                new FactionProfilerMonitor(),
                new GameLoopProfilerMonitor(),
                new GridProfilerMonitor(gridNameConflictSolver),
                new MethodNameProfilerMonitor(),
                new SessionComponentsProfilerMonitor(),
                new PlayerProfilerMonitor(playerNameConflictSolver),
                new ScriptProfilerMonitor(gridNameConflictSolver),
                new NetworkEventProfilerMonitor(),
                new PhysicsProfilerMonitor(),
                new PhysicsSimulateProfilerMonitor(),
                new PhysicsSimulateMtProfilerMonitor(),
            });
        }

        void OnGameLoaded()
        {
            TaskUtils.RunUntilCancelledAsync(_intervalRunner.LoopIntervals, _canceller.Token).Forget(Log);
        }

        void OnGameUnloading()
        {
            _intervalRunner?.Dispose();
            _config?.Dispose();
            _ipstackEndpoints?.Dispose();
            _canceller?.Cancel();
            _canceller?.Dispose();
        }

        public void ReloadConfig()
        {
            if (_config != null)
            {
                _config.Data.PropertyChanged -= OnConfigChanged;
            }

            var configFilePath = this.MakeConfigFilePath();
            _config?.Dispose();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);
            TorchMonitorConfig.Instance = _config.Data;
            _userControl?.Dispatcher.Invoke(() =>
            {
                _userControl.DataContext = TorchMonitorConfig.Instance;
                _userControl.InitializeComponent();
            });

            _fileLogger.Configure(TorchMonitorConfig.Instance);

            _config.Data.PropertyChanged += OnConfigChanged;

            Log.Info("reloaded configs");
        }

        void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            _fileLogger.Configure(TorchMonitorConfig.Instance);
        }
    }
}