using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Utils.General
{
    internal static class TaskUtils
    {
        public static async void Forget(this Task self, ILogger logger)
        {
            await self.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.Error(t.Exception);
                }
            });
        }

        public static Task MoveToThreadPool(CancellationToken canceller = default)
        {
            canceller.ThrowIfCancellationRequested();

            var taskSource = new TaskCompletionSource<byte>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                canceller.ThrowIfCancellationRequested();
                taskSource.SetResult(0);
            });

            return taskSource.Task;
        }

        public static Task RunUntilCancelledAsync(Func<CancellationToken, Task> f, CancellationToken canceller)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    await f(canceller);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }, canceller);
        }

        public static Task DelayMin(Stopwatch stopwatch, TimeSpan timeSpan, CancellationToken canceller = default)
        {
            var n = (timeSpan - stopwatch.Elapsed).Milliseconds;
            var m = Math.Max(n, 0);
            return Task.Delay(TimeSpan.FromMilliseconds(m), canceller);
        }

        public static async Task Timeout(this Task self, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completeTask = await Task.WhenAny(self, timeoutTask);
            if (completeTask != self)
            {
                throw new TimeoutException();
            }
        }
    }
}