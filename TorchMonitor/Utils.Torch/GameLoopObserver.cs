using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using Utils.General;

namespace Utils.Torch
{
    internal static class GameLoopObserver
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MySession), nameof(MySession.Update))]
        static readonly MethodInfo _sessionUpdateMethod;
#pragma warning restore 649

        static readonly ActionQueue _actionQueue;
        static bool _patched;

        static GameLoopObserver()
        {
            _actionQueue = new ActionQueue();
        }

        public static void Patch(PatchContext ptx)
        {
            var patchMethod = typeof(GameLoopObserver).GetMethod(nameof(OnSessionUpdate), BindingFlags.Static | BindingFlags.NonPublic);
            ptx.GetPattern(_sessionUpdateMethod).Suffixes.Add(patchMethod);
            _patched = true;
        }

        static void OnSessionUpdate()
        {
            _actionQueue.Flush(Log);
        }

        public static void OnNextUpdate(Action action)
        {
            if (!_patched)
            {
                throw new Exception("Not patched");
            }

            _actionQueue.Add(action);
        }

        public static Task MoveToGameLoop(CancellationToken canceller = default)
        {
            canceller.ThrowIfCancellationRequested();

            var taskSource = new TaskCompletionSource<byte>();

            OnNextUpdate(() =>
            {
                try
                {
                    canceller.ThrowIfCancellationRequested();
                    taskSource.TrySetResult(0);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            });

            return taskSource.Task;
        }
    }
}