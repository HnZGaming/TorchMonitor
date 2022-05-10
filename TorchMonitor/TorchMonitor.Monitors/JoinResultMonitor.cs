using System;
using InfluxDb.Torch;
using NLog;
using Sandbox.Engine.Multiplayer;
using TorchMonitor.Reflections;
using VRage.Network;
using VRage.Replication;

namespace TorchMonitor.Monitors
{
    public sealed class JoinResultMonitor : IDisposable
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public JoinResultMonitor()
        {
            MultiplayerManagerDedicated_UserRejected.OnJoinResponded += OnJoinResponded;
            MyDedicatedServerBase_ConnectionFailed.OnConnectionFailed += OnConnectionFailed;
        }

        public void Dispose()
        {
            MultiplayerManagerDedicated_UserRejected.OnJoinResponded -= OnJoinResponded;
            MyDedicatedServerBase_ConnectionFailed.OnConnectionFailed -= OnConnectionFailed;
        }

        void OnJoinResponded(ulong steamId, JoinResult failReason)
        {
            Write(steamId, failReason.ToString());
        }

        void OnConnectionFailed(ulong remoteUserId, string error)
        {
            Write(remoteUserId, error);
        }

        void Write(ulong steamId, string result)
        {
            var playerName = ((MyDedicatedServerBase)MyMultiplayerMinimalBase.Instance).GetMemberName(steamId);
            playerName ??= $"{steamId}";

            TorchInfluxDbWriter
                .Measurement("players_connectivity")
                .Tag("player_name", playerName)
                .Field("result", result)
                .Write();

            Log.Info($"join result: {steamId} {playerName} {result}");
        }
    }
}