using System;
using System.Collections.Concurrent;
using InfluxDb.Torch;
using Intervals;
using NLog;
using Sandbox.Engine.Multiplayer;
using TorchMonitor.Reflections;
using Utils.General;
using VRage.Network;
using VRage.Replication;

namespace TorchMonitor.Monitors
{
    public sealed class JoinResultMonitor : IIntervalListener, IDisposable
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly ConcurrentQueue<(ulong, JoinResult)> _joinResponses;

        public JoinResultMonitor()
        {
            _joinResponses = new ConcurrentQueue<(ulong, JoinResult)>();

            MultiplayerManagerDedicated_Patch.OnJoinResponded += OnJoinResponded;
        }

        public void Dispose()
        {
            _joinResponses.Clear();

            MultiplayerManagerDedicated_Patch.OnJoinResponded -= OnJoinResponded;
        }

        void OnJoinResponded(ulong steamId, JoinResult failReason)
        {
            _joinResponses.Enqueue((steamId, failReason));
        }

        public void OnInterval(int intervalsSinceStart)
        {
            if (intervalsSinceStart < TorchMonitorConfig.Instance.FirstIgnoredSeconds) return;
            if (intervalsSinceStart % 10 != 0) return;

            while (_joinResponses.TryDequeue(out var p))
            {
                var (steamId, result) = p;
                var playerName = ((MyDedicatedServerBase)MyMultiplayerMinimalBase.Instance).GetMemberName(steamId);
                playerName ??= $"{steamId}";

                TorchInfluxDbWriter
                    .Measurement("players_connectivity")
                    .Tag("player_name", playerName)
                    .Field("result", result.ToString())
                    .Write();

                Log.Debug($"join result: {steamId} {playerName} {result}");
            }
        }
    }
}