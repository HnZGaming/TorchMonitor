using System.Reflection;
using Torch.Managers.PatchManager;
using Torch.Server.Managers;
using VRage.Network;

namespace TorchMonitor.Reflections
{
    [PatchShim]
    public static class MultiplayerManagerDedicated_UserRejected
    {
        public delegate void JoinResponseDelegate(ulong steamId, JoinResult result);

        public static event JoinResponseDelegate OnJoinResponded;

        public static void Patch(PatchContext ctx)
        {
            ctx
                .GetPattern(typeof(MultiplayerManagerDedicated).GetMethod(nameof(UserRejected), BindingFlags.Instance | BindingFlags.NonPublic))
                .Suffixes
                .Add(typeof(MultiplayerManagerDedicated_UserRejected).GetMethod(nameof(UserRejected), BindingFlags.Static | BindingFlags.NonPublic));

            ctx
                .GetPattern(typeof(MultiplayerManagerDedicated).GetMethod(nameof(UserAccepted), BindingFlags.Instance | BindingFlags.NonPublic))
                .Suffixes
                .Add(typeof(MultiplayerManagerDedicated_UserRejected).GetMethod(nameof(UserAccepted), BindingFlags.Static | BindingFlags.NonPublic));
        }

        static void UserRejected(ulong steamId, JoinResult reason)
        {
            OnJoinResponded?.Invoke(steamId, reason);
        }

        static void UserAccepted(ulong steamId)
        {
            OnJoinResponded?.Invoke(steamId, JoinResult.OK);
        }
    }
}