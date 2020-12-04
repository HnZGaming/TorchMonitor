using System;

namespace Utils.General
{
    internal static class MathUtils
    {
        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }
}