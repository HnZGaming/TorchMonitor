using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Utils.General
{
    public sealed class CsvDb
    {
        readonly string _filePath;
        readonly char[] _separators;
        readonly string _separator;
        readonly Dictionary<string, string[]> _copy;

        public CsvDb(string filePath, char separator)
        {
            _filePath = filePath;
            _separators = new[] {separator};
            _separator = $"{separator}";
            _copy = new Dictionary<string, string[]>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Read()
        {
            _copy.Clear();

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "");
                return;
            }

            var lines = File.ReadAllLines(_filePath);
            foreach (var line in lines)
            {
                var elements = line.Split(_separators);
                var key = elements[0];
                var values = elements.Skip(1).ToArray();
                _copy.Add(key, values);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Write()
        {
            var tmpElements = new List<string>();
            var lines = new List<string>();
            foreach (var (key, values) in _copy)
            {
                tmpElements.Clear();
                tmpElements.Add(key);
                tmpElements.AddRange(values);
                lines.Add(string.Join(_separator, tmpElements));
            }

            File.WriteAllLines(_filePath, lines);
        }

        public bool HasValues(string key)
        {
            return _copy.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetValues(string key, out string[] values)
        {
            return _copy.TryGetValue(key, out values);
        }

        public IEnumerable<string[]> GetAllValues()
        {
            return _copy.Values;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InsertOrReplace(object key, IEnumerable<object> values)
        {
            _copy[key.ToString()] = values.Select(v => v.ToString()).ToArray();
        }
    }
}