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
        UserControl _userControl;

        CancellationTokenSource _canceller;
        IntervalRunner _intervalRunner;
        IpstackEndpoints _ipstackEndpoints;

        UserControl IWpfPlugin.GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

        public TorchMonitorNexus Nexus { get; private set; }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _canceller = new CancellationTokenSource();

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
            var configFilePath = this.MakeConfigFilePath();
            _config?.Dispose();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);
            TorchMonitorConfig.Instance = _config.Data;
            Log.Info("reloaded configs");
        }
    }
}