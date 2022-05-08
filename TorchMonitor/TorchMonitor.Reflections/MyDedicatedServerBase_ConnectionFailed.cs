using System.Reflection;
using Sandbox.Engine.Multiplayer;
using Torch.Managers.PatchManager;

namespace TorchMonitor.Reflections
{
    [PatchShim]
    public static class MyDedicatedServerBase_ConnectionFailed
    {
        public delegate void ConnectionFailedDelegate(ulong remoteUserId, string error);

        public static event ConnectionFailedDelegate OnConnectionFailed;

        public static void Patch(PatchContext ctx)
        {
            ctx
                .GetPattern(typeof(MyDedicatedServerBase).GetMethod(nameof(Peer2Peer_ConnectionFailed), BindingFlags.Instance | BindingFlags.NonPublic))
                .Suffixes
                .Add(typeof(MyDedicatedServerBase_ConnectionFailed).GetMethod(nameof(Peer2Peer_ConnectionFailed), BindingFlags.Static | BindingFlags.NonPublic));
        }

        static void Peer2Peer_ConnectionFailed(ulong remoteUserId, string error)
        {
            OnConnectionFailed?.Invoke(remoteUserId, error);
        }
    }
}