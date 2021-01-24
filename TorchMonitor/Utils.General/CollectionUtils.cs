using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Utils.General
{
    internal static class CollectionUtils
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

        public static bool TryGetFirst<T>(this IReadOnlyList<T> self, out T foundValue)
        {
            if (self.Count == 0)
            {
                foundValue = default;
                return false;
            }

            foundValue = self[0];
            return true;
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

        public static void Increment<K>(this IDictionary<K, int> self, K key)
        {
            self.TryGetValue(key, out var value);
            self[key] = value + 1;
        }

        public static string ToStringTable(this DataTable self)
        {
            var builder = new StringBuilder();

            foreach (DataColumn column in self.Columns)
            {
                builder.Append(column.ColumnName);
                builder.Append("  ");
            }

            builder.AppendLine();

            foreach (DataRow row in self.Rows)
            {
                foreach (var rowItem in row.ItemArray)
                {
                    builder.Append(rowItem);
                    builder.Append("  ");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public static T[] AsArray<T>(this IEnumerable<T> self)
        {
            return self is T[] selfArray ? selfArray : self.ToArray();
        }

        public static void AddRange<K, V>(this IDictionary<K, V> self, IReadOnlyDictionary<K, V> other)
        {
            foreach (var keyValuePair in other)
            {
                self[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        public static void AddRange<K, V>(this IDictionary<K, V> self, IEnumerable<(K, V)> other)
        {
            foreach (var (key, value) in other)
            {
                self[key] = value;
            }
        }

        public static IEnumerable<T> GroupSingletonBy<K, T>(this IEnumerable<T> self, Func<T, K> makeKey)
        {
            var dic = new HashSet<K>();
            foreach (var t in self)
            {
                var key = makeKey(t);
                if (!dic.Contains(key))
                {
                    dic.Add(key);
                    yield return t;
                }
            }
        }

        public static bool TryGetOrdinalByName(this DataColumnCollection self, string name, out int ordinal)
        {
            if (self.Contains(name))
            {
                var column = self[name];
                ordinal = column.Ordinal;
                return true;
            }

            ordinal = default;
            return false;
        }

        public static IReadOnlyDictionary<K, V> ToDictionary<K, V>(this IEnumerable<(K, V)> self)
        {
            return self.ToDictionary(p => p.Item1, p => p.Item2);
        }

        public static IEnumerable<T> GetExceptWith<T>(this IEnumerable<T> self, IEnumerable<T> other)
        {
            var selfSet = self as ISet<T> ?? new HashSet<T>(self);
            selfSet.ExceptWith(other);
            return selfSet;
        }

        public static void RemoveKeys<K, V>(this IDictionary<K, V> self, IEnumerable<K> keys)
        {
            foreach (var key in keys)
            {
                self.Remove(key);
            }
        }

        public static void Add<K, V, C>(this IDictionary<K, C> self, K key, V element) where C : ICollection<V>, new()
        {
            if (!self.TryGetValue(key, out var elements))
            {
                elements = new C();
                self[key] = elements;
            }

            elements.Add(element);
        }

        public static void AddOrReplace<K0, K1, V, D>(this IDictionary<K0, D> self, K0 key0, K1 key1, V element) where D : IDictionary<K1, V>, new()
        {
            if (!self.TryGetValue(key0, out var elements))
            {
                elements = new D();
                self[key0] = elements;
            }

            elements[key1] = element;
        }

        public static void IntersectWith<K, V>(this IDictionary<K, V> self, IEnumerable<K> other)
        {
            var otherSet = new HashSet<K>(other);
            foreach (var k in self.Keys.ToArray())
            {
                if (!otherSet.Contains(k))
                {
                    self.Remove(k);
                }
            }
        }
    }
}