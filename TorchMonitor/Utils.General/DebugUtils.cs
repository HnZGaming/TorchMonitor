using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    internal static class DebugUtils
    {
        public static string ToStringSeq<T>(this IEnumerable<T> self)
        {
            return $"[{string.Join(", ", self)}]";
        }

        public static void ThrowIfNull(this object self, string msg)
        {
            if (self == null)
            {
                throw new NullReferenceException(msg);
            }
        }

        public static T OrThrow<T>(this T self, string msg)
        {
            if (self == null)
            {
                throw new NullReferenceException(msg);
            }

            return self;
        }

        public static void ThrowIfNullOrEmpty(this string self, string msg)
        {
            if (string.IsNullOrEmpty(self))
            {
                throw new ArgumentException($"null or empty: {msg}");
            }
        }

        public static void ThrowIfNullOrEmpty<T>(this IEnumerable<T> self, string msg)
        {
            if (self == null || !self.Any())
            {
                throw new ArgumentException($"null or empty: {msg}");
            }
        }

        public static string HideCredential(this string credential, int visibleLength)
        {
            if (visibleLength > credential.Length)
            {
                visibleLength = credential.Length / 2;
            }

            var hiddenLength = credential.Length - visibleLength;
            var hiddenCredential = new string(Enumerable.Repeat('*', hiddenLength).ToArray());

            if (hiddenLength == credential.Length)
            {
                return hiddenCredential;
            }

            var visibleCredential = credential.Substring(hiddenLength);
            return hiddenCredential + visibleCredential;
        }
    }
}