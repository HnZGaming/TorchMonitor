using System;

namespace Utils.General
{
    internal static class LangUtils
    {
        public static TimeSpan Seconds(this int self) => TimeSpan.FromSeconds(self);
        public static TimeSpan Seconds(this double self) => TimeSpan.FromSeconds(self);

        public static string OrNull(this string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }
    }
}