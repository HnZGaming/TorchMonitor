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
        StupidDb _localDb;

        TorchMonitorConfig Config => _config.Data;

        public bool Enabled
        {
            set => Config.Enabled = value;
        }

        UserControl IWpfPlugin.GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _canceller = new CancellationTokenSource();

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);

            _ipstackEndpoints = new IpstackEndpoints(Config);

            var localDbFilePath = this.MakeFilePath($"{nameof(TorchMonitor)}.json");
            _localDb = new StupidDb(localDbFilePath);
            _localDb.Read();

            var playerOnlineTimeDb = new PlayerOnlineTimeDb(_localDb);
            playerOnlineTimeDb.Read();

            var gridNameConflictSolver = new NameConflictSolver();
            var playerNameConflictSolver = new NameConflictSolver();

            _intervalRunner = new IntervalRunner(Config, 1);
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(Config),
                //new GridMonitor(Config, gridNameConflictSolver),
                //new FloatingObjectsMonitor(Config),
                new RamUsageMonitor(Config),
                //new VoxelMonitor(),
                new OnlinePlayersMonitor(playerNameConflictSolver, playerOnlineTimeDb),
                new GeoLocationMonitor(_ipstackEndpoints, Config),
                new BlockTypeProfilerMonitor(Config),
                new FactionProfilerMonitor(Config),
                new GameLoopProfilerMonitor(Config),
                new GridProfilerMonitor(Config, Config, gridNameConflictSolver),
                new MethodNameProfilerMonitor(Config),
                new SessionComponentsProfilerMonitor(Config),
                new PlayerProfilerMonitor(Config, playerNameConflictSolver),
                new ScriptProfilerMonitor(Config, gridNameConflictSolver),
            });
        }

        void OnGameLoaded()
        {
            TaskUtils.RunUntilCancelledAsync(_intervalRunner.LoopIntervals, _canceller.Token).Forget(Log);
        }

        void OnGameUnloading()
        {
            _config?.Dispose();
            _ipstackEndpoints?.Dispose();
            _canceller?.Cancel();
            _canceller?.Dispose();
        }
    }
}