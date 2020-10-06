using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.InfluxDb;
using TorchMonitor.Business;
using TorchUtils.Utils;

namespace TorchMonitor.Views
{
    public class TMPlugin : TorchPluginBaseEx
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        readonly List<IMonitor> _monitors;
        CancellationTokenSource _canceller;
        InfluxDbClient _client;

        public TMPlugin()
        {
            _monitors = new List<IMonitor>();
        }

        public void Ping(string message)
        {
            _client.WritePing(message);
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
                new PlayersMonitor(_client),
                new GridMonitor(_client),
                new FloatingObjectsMonitor(_client),
                new RamUsageMonitor(_client),
                new CpuUsageMonitor(_client),
                new AsteroidMonitor(_client),
            });

            _client.WritePing("torch init");

            Task.Factory
                .StartNew(() => ObserveServerStat(token), token)
                .Forget(_logger);
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
                foreach (var monitor in _monitors)
                {
                    try
                    {
                        monitor.OnInterval(intervalSinceStart);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                }

                intervalSinceStart += 1;
                canceller.WaitHandle.WaitOne(TimeSpan.FromSeconds(1f));
            }
        }
    }
}