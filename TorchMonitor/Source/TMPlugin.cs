using System;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.InfluxDb;
using TorchMonitor.Business;
using TorchMonitor.Business.Monitors;
using TorchMonitor.Steam;
using TorchMonitor.Utils;

namespace TorchMonitor
{
    public class TMPlugin : TorchPluginBaseEx
    {
        const string ConfigFileName = "TorchMonitorConfig.config";
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        readonly IntervalRunner _intervalRunner;
        TorchMonitorConfig _config;

        public TMPlugin()
        {
            _intervalRunner = new IntervalRunner(1);
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            if (!TryFindConfigFile(ConfigFileName, out _config))
            {
                Log.Info("Creating a new TorchMonitorConfig file with default content");
                CreateConfigFile(ConfigFileName, new TorchMonitorConfig());

                TryFindConfigFile(ConfigFileName, out _config);
            }
        }

        protected override void OnGameLoaded()
        {
            var manager = Torch.Managers.GetManager<InfluxDbManager>();
            if (manager == null)
            {
                throw new Exception($"{nameof(InfluxDbManager)} not found");
            }

            var client = manager.Client;
            if (client == null)
            {
                throw new Exception("Manager found but client is not set");
            }
            
            var steamApiEndpoints = new SteamApiEndpoints(_config.SteamApiKey);

            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(client),
                new GridMonitor(client),
                new FloatingObjectsMonitor(client),
                new RamUsageMonitor(client),
                new AsteroidMonitor(client),
                new OnlinePlayersMonitor(client, steamApiEndpoints),
                new FactionConcealmentMonitor(client, _config),
            });

            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);
        }

        protected override void OnGameUnloading()
        {
            _intervalRunner.Dispose();
        }

        public IDisposable RunListener(IIntervalListener listener)
        {
            _intervalRunner.AddListener(listener);
            return new ActionDisposable(() => _intervalRunner.RemoveListener(listener));
        }
    }
}