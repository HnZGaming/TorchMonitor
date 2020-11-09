using System;
using System.Threading.Tasks;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using TorchMonitor.Monitors;
using TorchUtils;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase
    {
        const string ConfigFileName = "TorchMonitorConfig.config";
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        readonly IntervalRunner _intervalRunner;
        TorchMonitorConfig _config;
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

            if (!this.TryFindConfigFile(ConfigFileName, out _config))
            {
                Log.Info("Creating a new TorchMonitorConfig file with default content");
                this.CreateConfigFile(ConfigFileName, new TorchMonitorConfig());

                this.TryFindConfigFile(ConfigFileName, out _config);
            }

            _ipstackEndpoints = new IpstackEndpoints(_config.IpstackApiKey);
        }

        void OnGameLoaded()
        {
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(),
                new GridMonitor(),
                new FloatingObjectsMonitor(),
                new RamUsageMonitor(),
                new AsteroidMonitor(),
                new OnlinePlayersMonitor(_ipstackEndpoints),
                new FactionGridMonitor(_config),
            });

            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);
        }

        void OnGameUnloading()
        {
            _intervalRunner.Dispose();
        }
    }
}