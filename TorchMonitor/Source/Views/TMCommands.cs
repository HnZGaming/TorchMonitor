using NLog;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace TorchMonitor.Views
{
    [Category(ViewConsts.CommandCategory)]
    public class TMCommands : CommandModule
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        TMPlugin Plugin => (TMPlugin) Context.Plugin;

        [Command(ViewConsts.PingCommand, "Ping the database with a message.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Ping(string message)
        {
            Plugin.Ping(message);
        }
    }
}