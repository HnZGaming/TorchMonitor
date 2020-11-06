using System.Threading.Tasks;
using NLog;

namespace TorchUtils
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

        public static ThreadPoolTask MoveToThreadPool()
        {
            return new ThreadPoolTask();
        }
    }
}