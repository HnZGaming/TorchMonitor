using System;
using System.Collections.Generic;

namespace TorchMonitor.Utils
{
    public static class LinqUtils
    {
        public static bool TryGetFirst<T>(this IEnumerable<T> self, Func<T, bool> f, out T foundValue)
        {
            foreach (var t in self)
            {
                if (f(t))
                {
                    foundValue = t;
                    return true;
                }
            }

            foundValue = default;
            return false;
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> self, out T foundValue)
        {
            foreach (var t in self)
            {
                foundValue = t;
                return true;
            }

            foundValue = default;
            return false;
        }

        public static bool ContainsAny<T>(this ISet<T> self, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                if (self.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ToStringSeq<T>(this IEnumerable<T> self)
        {
            return $"[{string.Join(", ", self)}]";
        }
    }
}