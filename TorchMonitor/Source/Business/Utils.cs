using VRage.ModAPI;

namespace TorchMonitor.Business
{
    public static class Utils
    {
        public static bool IsConcealed(this IMyEntity entity)
        {
            // Concealment plugin uses `4` as a flag to prevent game from updating grids
            return (long) (entity.Flags & (EntityFlags) 4) != 0;
        }
    }
}