using Torch.Commands;
using VRageMath;

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
            if (Plugin.Start())
            {
                Context.Respond("Started monitoring");
            }
            else
            {
                Context.Respond("Failed starting monitoring", Color.Yellow);
            }
        }

        [Command(Cmd_Stop, "Stops monitoring.")]
        public void StopMonitoring()
        {
            if (Plugin.Stop())
            {
                Context.Respond("Stopped monitoring");
            }
            else
            {
                Context.Respond("Failed stopping monitoring", Color.Yellow);
            }
        }
    }
}