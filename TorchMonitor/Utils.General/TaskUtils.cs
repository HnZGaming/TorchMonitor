using System;
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

        public static Task MoveToThreadPool()
        {
            var taskSource = new TaskCompletionSource<byte>();
            ThreadPool.QueueUserWorkItem(_ => taskSource.SetResult(0));
            return taskSource.Task;
        }

        public static Task StartAsync(this CancellationTokenSource self, Action<CancellationToken> f)
        {
            return Task.Factory.StartNew(() => f(self.Token), self.Token);
        }

        public static bool WaitOneSafe(this WaitHandle self, TimeSpan timeSpan)
        {
            try
            {
                self.WaitOne(timeSpan);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }
}