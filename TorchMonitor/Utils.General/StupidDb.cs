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
    /// A database file can contain multiple tables.
    /// A table can contain multiple rows of a single type.
    /// A row type can contain arbitrary data as long as supported by JSON format.
    /// </summary>
    /// <remarks>
    /// Database file is human-readable JSON text, but should only be edited by this program.
    /// Any "manual" changes to the file during the runtime may be overwritten by this program.
    /// You can, however, edit the database file while the program is not running.
    /// </remarks>
    /// <remarks>
    /// A row type must contain exactly one ID string field that uniquely identifies the row.
    /// An ID string field can be specified by an attribute.
    /// </remarks>
    /// <remarks>
    /// Intended for a temporary and limited use only. Shouldn't be expected to process big data.
    /// </remarks>
    public sealed class StupidDb
    {
        /// <summary>
        /// Attribute to specify an ID property of a row type.
        /// Property type must be string and a row type must contain exactly one ID property.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public sealed class IdAttribute : Attribute
        {
        }

        readonly string _filePath;
        readonly Dictionary<Type, PropertyInfo> _cachedIdProperties;
        readonly Dictionary<string, JToken> _ramCopy;

        /// <summary>
        /// Instantiate with a path to the database file.
        /// </summary>
        /// <param name="filePath">Path to the database file.</param>
        public StupidDb(string filePath)
        {
            filePath.ThrowIfNullOrEmpty(nameof(filePath));

            _filePath = filePath;
            _cachedIdProperties = new Dictionary<Type, PropertyInfo>();
            _ramCopy = new Dictionary<string, JToken>();
        }

        /// <summary>
        /// Reset both RAM copy and database file to an empty state.
        /// </summary>
        public void Reset()
        {
            _ramCopy.Clear();
            Write();
        }

        /// <summary>
        /// Read the database file and cache it in the RAM.
        /// If the file is not found, create an empty JSON file.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Read()
        {
            _ramCopy.Clear();

            if (!File.Exists(_filePath))
            {
                var emptyText = JsonConvert.SerializeObject(_ramCopy);
                File.WriteAllText(_filePath, emptyText);
            }
            else
            {
                var fileText = File.ReadAllText(_filePath);
                var copy = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(fileText);
                _ramCopy.AddRange(copy);
            }
        }

        /// <summary>
        /// Get documents in specified table.
        /// </summary>
        /// <param name="tableName">Name of table.</param>
        /// <typeparam name="T">Type of documents in the table.</typeparam>
        /// <returns>Collection of documents in the table.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<T> Query<T>(string tableName)
        {
            if (!_ramCopy.TryGetValue(tableName, out var tableToken))
            {
                return Enumerable.Empty<T>();
            }

            var documents = tableToken.ToObject<Dictionary<string, T>>();
            return documents.Values;
        }

        /// <summary>
        /// Insert documents to specified table.
        /// </summary>
        /// <remarks>
        /// Existing documents will be overwritten by new documents with an identical ID string.
        /// </remarks>
        /// <param name="tableName">Name of the table to insert.</param>
        /// <param name="documents">Collection of documents to insert.</param>
        /// <typeparam name="T">Type of documents to insert.</typeparam>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Insert<T>(string tableName, IEnumerable<T> documents)
        {
            var table = GetTableObject<T>(tableName);

            foreach (var document in documents)
            {
                var id = GetId(document);
                table[id] = document;
            }

            var newTableToken = JToken.FromObject(table);
            _ramCopy[tableName] = newTableToken;
        }

        Dictionary<string, T> GetTableObject<T>(string tableName)
        {
            return _ramCopy.TryGetValue(tableName, out var tableToken)
                ? tableToken.ToObject<Dictionary<string, T>>()
                : new Dictionary<string, T>();
        }

        /// <summary>
        /// Write the RAM copy to the database file.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Write()
        {
            var text = JsonConvert.SerializeObject(_ramCopy, Formatting.Indented);
            File.WriteAllText(_filePath, text);
        }

        string GetId(object row)
        {
            var keyProperty = GetOrFindIdProperty(row.GetType());
            return (string) keyProperty.GetValue(row);
        }

        PropertyInfo GetOrFindIdProperty(Type type)
        {
            if (_cachedIdProperties.TryGetValue(type, out var idProperty))
            {
                return idProperty;
            }

            idProperty = FindIdProperty(type);
            _cachedIdProperties[type] = idProperty;

            return idProperty;
        }

        static PropertyInfo FindIdProperty(Type type)
        {
            var idPropertyInfo = (PropertyInfo) null;
            foreach (var propertyInfo in type.GetProperties())
            {
                var idAttrFound = false;
                foreach (var attr in propertyInfo.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(IdAttribute))
                    {
                        if (propertyInfo.PropertyType != typeof(string))
                        {
                            throw new Exception($"Type \"{type}\" contains an invalid ID property. ID must be of string");
                        }

                        idAttrFound = true;
                        break;
                    }
                }

                if (idAttrFound)
                {
                    if (idPropertyInfo != null)
                    {
                        throw new Exception($"Type \"{type}\" contains multiple ID properties");
                    }

                    idPropertyInfo = propertyInfo;
                }
            }

            if (idPropertyInfo == null)
            {
                throw new Exception($"Type \"{type}\" does not contain any ID properties");
            }

            return idPropertyInfo;
        }
    }
}