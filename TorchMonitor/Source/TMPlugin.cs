using System;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.InfluxDb;
using TorchMonitor.Business;
using TorchMonitor.Business.Monitors;
using TorchMonitor.Utils;

namespace TorchMonitor
{
    public class TMPlugin : TorchPluginBaseEx
    {
        const string ConfigFileName = "TorchMonitorConfig.config";
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        readonly IntervalRunner _intervalRunner;
        InfluxDbClient _client;
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

            _client = manager.Client;
            if (_client == null)
            {
                throw new Exception("Manager found but client is not set");
            }

            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(_client),
                new PlayerCountMonitor(_client),
                new GridMonitor(_client),
                new FloatingObjectsMonitor(_client),
                new RamUsageMonitor(_client),
                new AsteroidMonitor(_client),
                new PlayersMonitor(_client),
                new FactionConcealmentMonitor(_client, _config),
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