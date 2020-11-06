using System.Collections.Generic;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game.ModAPI;

namespace TorchUtils
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
    }
}