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
            return self.TryGetElementAt(0, out foundValue);
        }

        public static bool TryGetElementAt<T>(this IReadOnlyList<T> self, int index, out T foundValue)
        {
            if (self.Count < index + 1)
            {
                foundValue = default;
                return false;
            }

            foundValue = self[index];
            return true;
        }

        public static T GetFirstOrElse<T>(this IEnumerable<T> self, T defaultValue)
        {
            return self.TryGetFirst(out var t) ? t : defaultValue;
        }

        public static void Fill<T>(this IList<T> self, T element)
        {
            for (var i = 0; i < self.Count; i++)
            {
                self[i] = element;
            }
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

        public static void AddRangeWithKeys<K, V>(this IDictionary<K, V> self, IEnumerable<V> other, Func<V, K> makeKey)
        {
            foreach (var value in other)
            {
                self[makeKey(value)] = value;
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

        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<(K, V)> self)
        {
            return self.ToDictionary(p => p.Item1, p => p.Item2);
        }

        public static IEnumerable<(K Key, V Value)> ToTuples<K, V>(this IReadOnlyDictionary<K, V> self)
        {
            return self.Select(kv => (kv.Key, kv.Value));
        }

        public static IEnumerable<(K Key, V Value)> ToTuples<K, V>(this IEnumerable<KeyValuePair<K, V>> self)
        {
            return self.Select(kv => (kv.Key, kv.Value));
        }

        public static IEnumerable<T> GetExceptWith<T>(this IEnumerable<T> self, IEnumerable<T> other)
        {
            var selfSet = self as ISet<T> ?? new HashSet<T>(self);
            selfSet.ExceptWith(other);
            return selfSet;
        }

        public static void RemoveRange<K, V>(this IDictionary<K, V> self, IEnumerable<K> keys)
        {
            foreach (var key in keys)
            {
                self.Remove(key);
            }
        }

        public static void RemoveRangeExceptWith<K, V>(this IDictionary<K, V> self, IEnumerable<K> keys)
        {
            var keySet = keys as ISet<K> ?? new HashSet<K>(keys);
            foreach (var existingKey in self.Keys.ToArray())
            {
                if (!keySet.Contains(existingKey))
                {
                    self.Remove(existingKey);
                }
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

        public static HashSet<T> ToSet<T>(this IEnumerable<T> self)
        {
            return new HashSet<T>(self);
        }

        public static IEnumerable<T> Merge<T>(params IEnumerable<T>[] lists)
        {
            foreach (var list in lists)
            foreach (var x in list)
            {
                yield return x;
            }
        }

        public static IEnumerable<(T, int)> Indexed<T>(this IEnumerable<T> self)
        {
            return self.Select((t, i) => (t, i));
        }

        public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerable<T> self, IEnumerable<U> other)
        {
            return self.Zip(other, (t, u) => (t, u));
        }

        public static V GetOrElse<K, V>(this IReadOnlyDictionary<K, V> self, K key, V defaultValue)
        {
            return self.TryGetValue(key, out var v) ? v : defaultValue;
        }

#if !TORCH
        // ReSharper disable once UseDeconstructionOnParameter
        public static void Deconstruct<K, V>(this KeyValuePair<K, V> self, out K key, out V value)
        {
            key = self.Key;
            value = self.Value;
        }
#endif

        public static IReadOnlyDictionary<K, (V0 Value0, V1 Value1)> Zip<K, V0, V1>(
            this IReadOnlyDictionary<K, V0> self,
            IReadOnlyDictionary<K, V1> other,
            V0 defaultValue0 = default,
            V1 defaultValue1 = default)
        {
            var result = new Dictionary<K, (V0, V1)>();
            foreach (var (k, v0) in self)
            {
                var v1 = other.GetOrElse(k, defaultValue1);
                result[k] = (v0, v1);
            }

            foreach (var (k, v1) in other)
            {
                var v0 = self.GetOrElse(k, defaultValue0);
                result[k] = (v0, v1);
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<K, V1>> SelectValue<K, V0, V1>(this IReadOnlyDictionary<K, V0> self, Func<V0, V1> f)
        {
            return self.Select(p => new KeyValuePair<K, V1>(p.Key, f(p.Value)));
        }
    }
}