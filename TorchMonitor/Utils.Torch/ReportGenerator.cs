using System;
using System.Threading.Tasks;
using NLog;
using Torch.Commands;
using Utils.General;
using VRageMath;

namespace Utils.Torch
{
    internal static class ReportGenerator
    {
        readonly static Random _numberGenerator = new Random();

        public static void LogAndRespond(object self, Exception e, Action<string> respond)
        {
            var errorId = $"{_numberGenerator.Next(0, 999999):000000}";
            self.GetFullNameLogger().Error(e, errorId);
            respond($"Oops, something broke. #{errorId}. Cause: \"{e.Message}\".");
        }
    }
}