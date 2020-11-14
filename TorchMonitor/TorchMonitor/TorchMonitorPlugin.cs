using System.Threading.Tasks;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchUtils;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IntervalRunner _intervalRunner;

        Persistent<TorchMonitorConfig> _config;
        UserControl _userControl;

        IpstackEndpoints _ipstackEndpoints;

        public TorchMonitorPlugin()
        {
            _intervalRunner = new IntervalRunner(1);
        }

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
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(),
                new GridMonitor(),
                new FloatingObjectsMonitor(),
                new RamUsageMonitor(),
                new VoxelMonitor(),
                new OnlinePlayersMonitor(),
                new GeoLocationMonitor(_ipstackEndpoints, _config.Data),
            });

            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);
            
            Log.Info("Started interval");
        }

        void OnGameUnloading()
        {
            _intervalRunner.Dispose();
            _ipstackEndpoints.Dispose();
        }
    }
}