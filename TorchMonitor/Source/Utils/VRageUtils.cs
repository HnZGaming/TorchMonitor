using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace TorchMonitor.Utils
{
    public static class VRageUtils
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
    }
}