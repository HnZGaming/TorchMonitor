using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using Intervals;
using Ipstack;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using TorchMonitor.Monitors;
using TorchMonitor.ProfilerMonitors;
using TorchMonitor.Utils;
using Utils.General;
using Utils.Torch;

namespace TorchMonitor
{
    public class TorchMonitorPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchMonitorConfig> _config;
        TorchMonitorControl _userControl;

        CancellationTokenSource _canceller;
        IntervalRunner _intervalRunner;
        IpstackEndpoints _ipstackEndpoints;
        FileLoggingConfigurator _fileLogger;
        GeoLocationCollection _geoLocationCollection;
        JoinResultMonitor _joinResultMonitor;

        UserControl IWpfPlugin.GetControl()
        {
            return _userControl ??= new TorchMonitorControl(this);
        }

        public TorchMonitorNexus Nexus { get; private set; }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _canceller = new CancellationTokenSource();

            _fileLogger = new FileLoggingConfigurator(
                nameof(TorchMonitor),
                new[]
                {
                    $"{nameof(TorchMonitor)}.*",
                    $"{nameof(Intervals)}.*",
                },
                TorchMonitorConfig.DefaultLogPath);
            _fileLogger.Initialize();

            LoadConfig();

            _ipstackEndpoints = new IpstackEndpoints();

            var localDbFilePath = this.MakeFilePath($"{nameof(TorchMonitor)}.json");
            var playerOnlineTimeDb = new PlayerOnlineTimeDb(localDbFilePath);
            playerOnlineTimeDb.Read();

            var gridNameConflictSolver = new NameConflictSolver<long>();
            var playerNameConflictSolver = new NameConflictSolver<ulong>();
            Nexus = new TorchMonitorNexus();

            _geoLocationCollection = new GeoLocationCollection(_ipstackEndpoints);

            var listeners = new Dictionary<string, IIntervalListener>
            {
                { "server_sync", new SyncMonitor() },
                { "blocks_all, grids_all, blocks_grids, blocks_players, blocks_factions, concealment", new GridMonitor() },
                { "floating_objects", new FloatingObjectsMonitor() },
                { "resource (ram)", new RamUsageMonitor() },
                { "voxels", new VoxelMonitor() },
                { "ping", new PingMonitor() },
                { "players_players, players_factions, server, nexus", new OnlinePlayersMonitor(playerNameConflictSolver, playerOnlineTimeDb, Nexus) },
                { "players_continents, players_countries", new GeoLocationMonitor(_geoLocationCollection) },
                { "profiler_block_types", new BlockTypeProfilerMonitor() },
                { "profiler_entity_types", new EntityTypeProfilerMonitor() },
                { "profiler_factions", new FactionProfilerMonitor() },
                { "profiler_game_loop", new GameLoopProfilerMonitor() },
                { "profiler", new GridProfilerMonitor(gridNameConflictSolver) },
                { "methods", new MethodNameProfilerMonitor() },
                { "profiler_game_loop_session_components", new SessionComponentsProfilerMonitor() },
                { "profiler_players", new PlayerProfilerMonitor(playerNameConflictSolver) },
                { "profiler_scripts", new ScriptProfilerMonitor(gridNameConflictSolver) },
                { "profiler_network_events", new NetworkEventProfilerMonitor() },
                { "profiler_physics_grids", new PhysicsProfilerMonitor() },
                { "profiler_physics_simulate", new PhysicsSimulateProfilerMonitor() },
                { "profiler_physics_simulate_mt", new PhysicsSimulateMtProfilerMonitor() },
                { "players_pings", new ClientPingMonitor(_geoLocationCollection) },
            };

            _intervalRunner = new IntervalRunner(1);
            _intervalRunner.AddListeners(listeners);

            // make sure all the features are listed up in the config
            _config.Data.InitializeFeatureCollection(listeners.Select(p => p.Key));

            _joinResultMonitor = new JoinResultMonitor();

            OnConfigChanged(null, null);
        }

        void OnGameLoaded()
        {
            TaskUtils.RunUntilCancelledAsync(_intervalRunner.LoopIntervals, _canceller.Token).Forget(Log);
        }

        void OnGameUnloading()
        {
            _intervalRunner?.Dispose();
            _config?.Dispose();
            _ipstackEndpoints?.Dispose();
            _canceller?.Cancel();
            _canceller?.Dispose();
            _joinResultMonitor?.Dispose();
        }

        void LoadConfig()
        {
            if (_config != null)
            {
                PropertyChangedEventManager.RemoveHandler(_config.Data, OnConfigChanged, "");
            }

            var configFilePath = this.MakeFilePath($"{nameof(TorchMonitorPlugin)}.cfg");
            _config?.Dispose();
            _config = Persistent<TorchMonitorConfig>.Load(configFilePath);
            PropertyChangedEventManager.AddHandler(_config.Data, OnConfigChanged, "");

            TorchMonitorConfig.Instance = _config.Data;

            _userControl?.Dispatcher.Invoke(() =>
            {
                _userControl.DataContext = TorchMonitorConfig.Instance;
                _userControl.InitializeComponent();
            });
        }

        public void ReloadConfig()
        {
            LoadConfig();
            OnConfigChanged(null, null);
            Log.Info("reloaded configs");
        }

        void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            _fileLogger.Configure(TorchMonitorConfig.Instance);

            if ((e?.PropertyName ?? "") is "" or nameof(TorchMonitorConfig.Features))
            {
                foreach (var (name, enabled) in _config.Data.Features)
                {
                    _intervalRunner.SetEnabled(name, enabled);
                }
            }
        }

        public IEnumerable<(string, bool)> GetFeatures()
        {
            return _intervalRunner.GetListeners();
        }

        public void SetFeatureEnabled(string name, bool enabled)
        {
            _intervalRunner.SetEnabled(name, enabled);
            _config.Data.SetFeatureEnabled(name, enabled);
        }
    }
}