using System;
using System.Threading.Tasks;
using NLog;
using Torch.Commands;
using VRageMath;

namespace Utils.Torch
{
    internal static class CommandErrorResponseGenerator
    {
        readonly static Random Random = new Random();

        public static void CatchAndReport(this CommandModule self, Action f)
        {
            try
            {
                f();
            }
            catch (Exception e)
            {
                LogAndRespond(self, e);
            }
        }

        public static async void CatchAndReport(this CommandModule self, Func<Task> f)
        {
            try
            {
                await f();
            }
            catch (Exception e)
            {
                LogAndRespond(self, e);
            }
        }

        static void LogAndRespond(CommandModule self, Exception e)
        {
            var errorId = $"{Random.Next(0, 999999):000000}";
            self.GetFullNameLogger().Error(e, errorId);
            self.Context.Respond($"Oops, something broke. #{errorId}. Cause: \"{e.Message}\".", Color.Red);
        }

        static ILogger GetFullNameLogger(this object self)
        {
            return LogManager.GetLogger(self.GetType().FullName);
        }
    }
}