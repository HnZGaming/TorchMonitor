using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.ModAPI;

namespace TorchUtils
{
    internal static class VRageUtils
    {
        public static IEnumerable<long> Owners(this IMyCubeGrid self)
        {
            var ownerIds = new HashSet<long>();
            foreach (var owner in self.BigOwners)
            {
                ownerIds.Add(owner);
            }

            foreach (var owner in self.SmallOwners)
            {
                ownerIds.Add(owner);
            }

            return ownerIds;
        }

        public static ulong SteamId(this MyPlayer p)
        {
            return p.Id.SteamId;
        }

        public static long PlayerId(this MyPlayer p)
        {
            return p.Identity.IdentityId;
        }

        public static MyCubeGrid GetTopGrid(this IEnumerable<MyCubeGrid> group)
        {
            return group.MaxBy(g => g.Mass);
        }

        public static ISet<long> BigOwnersSet(this IEnumerable<MyCubeGrid> group)
        {
            return new HashSet<long>(group.SelectMany(g => g.BigOwners));
        }

        public static bool IsConcealed(this IMyEntity entity)
        {
            // Concealment plugin uses `4` as a flag to prevent game from updating grids
            return (long) (entity.Flags & (EntityFlags) 4) != 0;
        }

        public static MyCubeGrid GetBiggestGrid(this IEnumerable<MyCubeGrid> grids)
        {
            var myCubeGrid = (MyCubeGrid) null;
            var num = 0.0;
            foreach (var grid in grids)
            {
                var volume = grid.PositionComp.WorldAABB.Size.Volume;
                if (volume > num)
                {
                    num = volume;
                    myCubeGrid = grid;
                }
            }

            return myCubeGrid;
        }

        public static bool OwnsAll(this IMyPlayer player, IEnumerable<MyCubeGrid> grids)
        {
            // ownership check
            foreach (var grid in grids)
            {
                if (!grid.BigOwners.Any()) continue;
                if (!grid.BigOwners.Contains(player.IdentityId)) return false;
            }

            return true;
        }

        public static bool IsAllActionAllowed(this MyEntity self)
        {
            return MySessionComponentSafeZones.IsActionAllowed(self, MySafeZoneAction.All);
        }

        public static ulong GetAdminSteamId()
        {
            if (!MySandboxGame.ConfigDedicated.Administrators.TryGetFirst(out var adminSteamIdStr)) return 0L;
            if (!ulong.TryParse(adminSteamIdStr, out var adminSteamId)) return 0L;
            return adminSteamId;
        }
    }
}