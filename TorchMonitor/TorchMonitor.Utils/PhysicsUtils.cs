using System.Linq;
using Havok;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace TorchMonitor.Utils
{
    public static class PhysicsUtils
    {
        public static bool TryGetHeaviestGrid(HkWorld world, out IMyCubeGrid heaviestGrid)
        {
            var grids = world
                .GetEntities()
                .ToArray()
                .Where(e => e is IMyCubeGrid)
                .Cast<IMyCubeGrid>()
                .Where(e => e.IsTopMostParent())
                .ToArray();

            if (!grids.Any())
            {
                heaviestGrid = null;
                return false;
            }

            heaviestGrid = grids.OrderByDescending(g => g.Physics.Mass).First();
            return true;
        }
    }
}