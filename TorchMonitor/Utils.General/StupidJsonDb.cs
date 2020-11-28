using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utils.General
{
    /// <summary>
    /// Stupid implementation of a single-file, document-based database.
    /// A database file contains multiple tables.
    /// A table contains multiple rows of a single type.
    /// A row (type) contains exactly one key field and arbitrary data.
    /// Will overwrite any changes to the file by other parties.
    /// Thread-safe.
    /// </summary>
    public sealed class StupidJsonDb
    {
        readonly string _filePath;
        readonly Dictionary<Type, PropertyInfo> _cachedKeyProperties;
        Dictionary<string, JToken> _ramDb;

        public StupidJsonDb(string filePath)
        {
            _filePath = filePath;
            _cachedKeyProperties = new Dictionary<Type, PropertyInfo>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            if (!File.Exists(_filePath))
            {
                _ramDb = new Dictionary<string, JToken>();
                var emptyText = JsonConvert.SerializeObject(_ramDb);
                File.WriteAllText(_filePath, emptyText);
            }
            else
            {
                var fileText = File.ReadAllText(_filePath);
                _ramDb = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(fileText);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<T> Query<T>(string tableName)
        {
            if (!_ramDb.TryGetValue(tableName, out var tableToken))
            {
                return Enumerable.Empty<T>();
            }

            var rows = tableToken.ToObject<Dictionary<string, T>>();
            return rows.Values;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Insert<T>(string tableName, IEnumerable<T> rows)
        {
            var table = _ramDb.TryGetValue(tableName, out var tableToken)
                ? tableToken.ToObject<Dictionary<string, T>>()
                : new Dictionary<string, T>();

            foreach (var row in rows)
            {
                var key = GetKey(row);
                table[key] = row;
            }

            var newTableToken = JToken.FromObject(table);
            _ramDb[tableName] = newTableToken;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Write()
        {
            var text = JsonConvert.SerializeObject(_ramDb, Formatting.Indented);
            File.WriteAllText(_filePath, text);
        }

        string GetKey(object row)
        {
            var keyProperty = GetOrFindKeyProperty(row.GetType());
            return (string) keyProperty.GetValue(row);
        }

        PropertyInfo GetOrFindKeyProperty(Type type)
        {
            if (_cachedKeyProperties.TryGetValue(type, out var keyProperty))
            {
                return keyProperty;
            }

            keyProperty = FindKeyProperty(type);
            _cachedKeyProperties[type] = keyProperty;

            return keyProperty;
        }

        static PropertyInfo FindKeyProperty(Type type)
        {
            var keyPropertyInfo = (PropertyInfo) null;
            foreach (var propertyInfo in type.GetProperties())
            {
                var keyAttrFound = false;
                foreach (var attr in propertyInfo.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(StupidJsonDbKeyAttribute))
                    {
                        keyAttrFound = true;
                        break;
                    }
                }

                if (keyAttrFound)
                {
                    if (keyPropertyInfo != null)
                    {
                        throw new Exception($"Type \"{type}\" contains multiple key properties");
                    }

                    keyPropertyInfo = propertyInfo;
                }
            }

            if (keyPropertyInfo == null)
            {
                throw new Exception($"Type \"{type}\" does not contain any key properties");
            }

            if (keyPropertyInfo.PropertyType != typeof(string))
            {
                throw new Exception($"Type \"{type}\" contains an invalid key. Key must be of string");
            }

            return keyPropertyInfo;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StupidJsonDbKeyAttribute : Attribute
    {
    }
}