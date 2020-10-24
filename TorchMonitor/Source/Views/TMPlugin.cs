using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.InfluxDb;
using Torch.Server.Utils;
using TorchMonitor.Business;

namespace TorchMonitor.Views
{
    public class TMPlugin : TorchPluginBaseEx
    {
        const string ConfigFileName = "TMConfig.config";
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        readonly List<IMonitor> _monitors;
        CancellationTokenSource _canceller;
        InfluxDbClient _client;
        TMConfig _config;

        public TMPlugin()
        {
            _monitors = new List<IMonitor>();
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            if (!TryFindConfigFile(ConfigFileName, out _config))
            {
                Log.Info("Creating a new TMConfig file with default content");
                CreateConfigFile(ConfigFileName, new TMConfig());

                TryFindConfigFile(ConfigFileName, out _config);
            }
        }

        protected override void OnGameLoaded()
        {
            _canceller = new CancellationTokenSource();
            var token = _canceller.Token;

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

            _monitors.AddRange(new IMonitor[]
            {
                new ServerStatMonitor(_client),
                new SyncMonitor(_client),
                new PlayerCountMonitor(_client),
                new GridMonitor(_client),
                //new FloatingObjectsMonitor(_client),
                new RamUsageMonitor(_client),
                new CpuUsageMonitor(_client),
                //new AsteroidMonitor(_client),
                //new WelderMonitor(_client),
                new PlayersMonitor(_client),
                new FactionConcealmentMonitor(_client, _config),
            });

            _client.WritePing("torch init");

            Task.Factory
                .StartNew(() => ObserveServerStat(token), token)
                .Forget(Log);
        }

        protected override void OnGameUnloading()
        {
            _canceller?.Cancel();
            _canceller?.Dispose();
            _canceller = null;

            _client.WritePing("session unloaded");
        }

        void ObserveServerStat(CancellationToken canceller)
        {
            _client.WritePing("session loaded");

            var intervalSinceStart = 0;

            while (!canceller.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                var intervalsSinceStartCopy = intervalSinceStart;
                Parallel.ForEach(_monitors, monitor =>
                {
                    try
                    {
                        monitor.OnInterval(intervalsSinceStartCopy);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });

                intervalSinceStart += 1;

                var spentTime = (DateTime.UtcNow - startTime).TotalSeconds;
                if (spentTime > 1f)
                {
                    Log.Warn($"Monitor spent more than 1 second: {spentTime}s");
                    continue;
                }
                
                var waitTime = 1f - spentTime;
                canceller.WaitHandle.WaitOne(TimeSpan.FromSeconds(waitTime));
            }
        }
    }
}