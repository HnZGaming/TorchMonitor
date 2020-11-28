using System.Threading.Tasks;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchMonitor.Monitors.Profilers;
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
        bool _started;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(() => OnGameLoaded());
            this.ListenOnGameUnloading(() => OnGameUnloading());

            var configFilePath = this.MakeConfigFilePath();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);

            _ipstackEndpoints = new IpstackEndpoints(_config.Data);

            Log.Info("Initialized plugin");
        }

        public UserControl GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

        void OnGameLoaded()
        {
            Start();
        }

        public bool Start()
        {
            if (_started)
            {
                Log.Warn("Aborted starting a process; already started");
                return false;
            }

            _started = true;

            _intervalRunner = new IntervalRunner(_config.Data, 1);
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(_config.Data),
                new GridMonitor(_config.Data),
                new FloatingObjectsMonitor(_config.Data),
                new RamUsageMonitor(_config.Data),
                new VoxelMonitor(),
                new OnlinePlayersMonitor(),
                new GeoLocationMonitor(_ipstackEndpoints, _config.Data),
                new BlockTypeProfilerMonitor(_config.Data),
                new FactionProfilerMonitor(_config.Data),
                new GameLoopProfilerMonitor(_config.Data),
                new GridProfilerMonitor(_config.Data),
                new MethodNameProfilerMonitor(_config.Data),
                new SessionComponentsProfilerMonitor(_config.Data),
            });

            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);

            Log.Info("Started interval");
            return true;
        }

        public bool Stop()
        {
            if (_intervalRunner == null)
            {
                Log.Warn("Aborted stopping a process; not running");
                return false;
            }

            _intervalRunner?.Dispose();
            _intervalRunner = null;
            _started = false;
            return true;
        }

        void OnGameUnloading()
        {
            Stop();
            _ipstackEndpoints.Dispose();
        }
    }
}