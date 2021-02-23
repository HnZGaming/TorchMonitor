using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Targets;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game.ModAPI;

namespace Utils.Torch
{
    internal static class TorchUtils
    {
        public static IEnumerable<Command> GetPluginCommands(this ITorchBase self, string category, MyPromoteLevel? promoteLevel)
        {
            var syntaxPrefix = $"!{category}";
            var commandManager = self.CurrentSession.Managers.GetManager<CommandManager>();
            foreach (var node in commandManager.Commands.WalkTree())
            {
                if (!node.IsCommand) continue;
                if (node.Command.MinimumPromoteLevel > promoteLevel) continue;
                if (!node.Command.SyntaxHelp.StartsWith(syntaxPrefix)) continue;

                yield return node.Command;
            }
        }

        public static Target GetWpfTarget()
        {
            return LogManager.Configuration.AllTargets.First(t => t.Name == "wpf");
        }

        public static void SendMessage(this IChatManagerServer self, string name, ulong targetSteamId, string message)
        {
            self.SendMessageAsOther(name, message, targetSteamId: targetSteamId);
        }
    }
}