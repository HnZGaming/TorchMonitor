using Torch.Commands;

namespace TorchMonitor
{
    [Category(Category)]
    public sealed class TorchMonitorCommandModule : CommandModule
    {
        const string Category = "monitor";
        const string Cmd_Start = "on";
        const string Cmd_Stop = "off";

        TorchMonitorPlugin Plugin => (TorchMonitorPlugin) Context.Plugin;

        [Command(Cmd_Start, "Starts monitoring.")]
        public void StartMonitoring()
        {
            Plugin.Enabled = true;
        }

        [Command(Cmd_Stop, "Stops monitoring.")]
        public void StopMonitoring()
        {
            Plugin.Enabled = false;
        }
    }
}