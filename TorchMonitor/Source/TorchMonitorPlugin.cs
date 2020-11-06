﻿using System;
using System.Threading.Tasks;
using NLog;
using Torch.API;
using TorchMonitor.Business;
using TorchMonitor.Business.Monitors;
using TorchMonitor.Ipstack;
using TorchMonitor.Steam;
using TorchMonitor.Utils;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBaseEx
    {
        const string ConfigFileName = "TorchMonitorConfig.config";
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        readonly IntervalRunner _intervalRunner;
        TorchMonitorConfig _config;
        SteamApiEndpoints _steamApiEndpoints;
        IpstackEndpoints _ipstackEndpoints;

        public TorchMonitorPlugin()
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

            _steamApiEndpoints = new SteamApiEndpoints(_config.SteamApiKey);
            _ipstackEndpoints = new IpstackEndpoints(_config.IpstackApiKey);
        }

        protected override void OnGameLoaded()
        {
            _intervalRunner.AddListeners(new IIntervalListener[]
            {
                new SyncMonitor(),
                new GridMonitor(),
                new FloatingObjectsMonitor(),
                new RamUsageMonitor(),
                new AsteroidMonitor(),
                new OnlinePlayersMonitor(_steamApiEndpoints, _ipstackEndpoints),
                new FactionConcealmentMonitor(_config),
            });

            Task.Factory
                .StartNew(_intervalRunner.RunIntervals)
                .Forget(Log);
        }

        protected override void OnGameUnloading()
        {
            _intervalRunner.Dispose();
            _steamApiEndpoints?.Dispose();
        }

        public IDisposable RunListener(IIntervalListener listener)
        {
            _intervalRunner.AddListener(listener);
            return new ActionDisposable(() => _intervalRunner.RemoveListener(listener));
        }
    }
}