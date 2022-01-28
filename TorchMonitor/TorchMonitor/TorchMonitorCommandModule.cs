using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

namespace TorchMonitor
{
    [Category("tm")]
    public sealed class TorchMonitorCommandModule : CommandModule
    {
        TorchMonitorPlugin Plugin => (TorchMonitorPlugin)Context.Plugin;

        [Command("on", "Starts monitoring")]
        [Permission(MyPromoteLevel.Admin)]
        public void StartMonitoring()
        {
            Plugin.Enabled = true;
        }

        [Command("off", "Stops monitoring")]
        [Permission(MyPromoteLevel.Admin)]
        public void StopMonitoring()
        {
            Plugin.Enabled = false;
        }

        [Command("nexus", "Shows Nexus help")]
        [Permission(MyPromoteLevel.Admin)]
        public void ShowNexusHelp()
        {
            var adminId = GetCallerPlayer().IdentityId;
            var msg = new StringBuilder();
            msg.AppendLine("!tm nexus corners -- shows 8 corners of the monitored nexus sector");
            msg.AppendLine("!tm nexus centers -- shows the center position of all monitored segments");

            MyVisualScriptLogicProvider.SendChatMessage(msg.ToString(), "", adminId);
        }

        [Command("nexus corners", "Shows 8 corners of the monitored nexus sector")]
        [Permission(MyPromoteLevel.Admin)]
        public void ShowNexusSectorCorners()
        {
            var adminId = GetCallerPlayer().IdentityId;
            var corners = Plugin.Nexus.GetCorners();
            for (var i = 0; i < corners.Count; i++)
            {
                var corner = corners[i];
                var name = $"!tm nexus corners {i}";
                MyVisualScriptLogicProvider.AddGPS(name, "", corner, Color.Magenta, playerId: adminId);
            }
        }

        [Command("nexus centers", "Shows the center of all monitored nexus segments")]
        [Permission(MyPromoteLevel.Admin)]
        public void ShowNexusSegmentCenters()
        {
            var adminId = GetCallerPlayer().IdentityId;
            foreach (var (index, center) in Plugin.Nexus.GetCenters())
            {
                var name = $"!tm nexus centers {index.X}_{index.Y}_{index.Z}";
                MyVisualScriptLogicProvider.AddGPS(name, "", center, Color.Magenta, playerId: adminId);
            }
        }

        IMyPlayer GetCallerPlayer()
        {
            if (!(Context.Player is { } admin))
            {
                throw new InvalidOperationException("must be called in game");
            }

            return admin;
        }
    }
}