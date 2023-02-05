using System;
using System.Diagnostics;
using System.Reflection;
using NLog;
using Sandbox.Game.Entities;
using Torch.Managers.PatchManager;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace TorchMonitor.Reflections
{
    [PatchShim]
    public static class MyCubeGrid_Close
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static void Patch(PatchContext ctx)
        {
            ctx
                .GetPattern(typeof(MyEntity).GetMethod(nameof(MyEntity.Close), BindingFlags.Instance | BindingFlags.Public))
                .Prefixes
                .Add(typeof(MyCubeGrid_Close).GetMethod(nameof(Close), BindingFlags.Static | BindingFlags.NonPublic));
        }

        static void Close(MyEntity __instance)
        {
            if (__instance is MyCubeGrid)
            {
                Log.Info($"'{__instance.DisplayName}' ({__instance.EntityId}) closed\n{new StackTrace()}");
            }
        }
    }
}