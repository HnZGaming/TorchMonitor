using System.Threading;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchUtils;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase, IWpfPlugin
    {
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

            ThreadPool.QueueUserWorkItem(_ =>
            {
                _intervalRunner.RunIntervals();
            });
        }

        void OnGameUnloading()
        {
            _intervalRunner.Dispose();
            _ipstackEndpoints.Dispose();
        }
    }
}