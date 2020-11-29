using System.Threading.Tasks;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchMonitor.ProfilerMonitors;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchMonitorConfig> _config;
        UserControl _userControl;

        IntervalRunner _intervalRunner;
        IpstackEndpoints _ipstackEndpoints;
        StupidDb _localDb;

        TorchMonitorConfig Config => _config.Data;

        public bool Enabled
        {
            set => Config.Enabled = value;
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);

            _ipstackEndpoints = new IpstackEndpoints(Config);

            var localDbFilePath = this.MakeFilePath($"{nameof(TorchMonitor)}.json");
            _localDb = new StupidDb(localDbFilePath);

            if (Config.ResetLocalDatabaseOnNextStart)
            {
                _localDb.Reset();
                Config.ResetLocalDatabaseOnNextStart = false;
            }
            else
            {
                _localDb.Read();
            }

            var playerOnlineTimeDb = new PlayerOnlineTimeDb(_localDb);
            playerOnlineTimeDb.Read();

            _intervalRunner = new IntervalRunner(Config, 1);
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(Config),
                new GridMonitor(Config, new NameConflictSolver()),
                new FloatingObjectsMonitor(Config),
                new RamUsageMonitor(Config),
                new VoxelMonitor(),
                new OnlinePlayersMonitor(new NameConflictSolver(), playerOnlineTimeDb),
                new GeoLocationMonitor(_ipstackEndpoints, Config),
                new BlockTypeProfilerMonitor(Config),
                new FactionProfilerMonitor(Config),
                new GameLoopProfilerMonitor(Config),
                new GridProfilerMonitor(Config),
                new MethodNameProfilerMonitor(Config),
                new SessionComponentsProfilerMonitor(Config),
            });

            Config.PropertyChanged += (sender, args) =>
            {
                _intervalRunner.Enabled = Config.Enabled;
            };
        }

        public UserControl GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

        void OnGameLoaded()
        {
            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);
        }

        void OnGameUnloading()
        {
            _config?.Dispose();
            _intervalRunner?.Dispose();
            _ipstackEndpoints?.Dispose();
        }
    }
}