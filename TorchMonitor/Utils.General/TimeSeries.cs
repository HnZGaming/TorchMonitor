using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Utils.General
{
    internal sealed class TimeSeries<T>
    {
        class TimestampComparer : IComparer<DateTime>
        {
            public int Compare(DateTime x, DateTime y)
            {
                return x.ToBinary().CompareTo(y.ToBinary());
            }
        }

        readonly SortedList<DateTime, List<T>> _timeline;

        public TimeSeries()
        {
            _timeline = new SortedList<DateTime, List<T>>(new TimestampComparer());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(DateTime timestamp, IEnumerable<T> elements)
        {
            if (!_timeline.TryGetValue(timestamp, out var list))
            {
                list = new List<T>();
                _timeline[timestamp] = list;
            }

            list.AddRange(elements);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RemoveOlderThan(DateTime thresholdTimestamp)
        {
            if (!_timeline.Any()) return;

            var removedTimestamps = new List<DateTime>();
            foreach (var timestamp in _timeline.Keys.ToArray())
            {
                if (timestamp > thresholdTimestamp) break;

                removedTimestamps.Add(timestamp);
            }

            foreach (var removedTimestamp in removedTimestamps)
            {
                _timeline.Remove(removedTimestamp);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<(DateTime, T)> GetElementsSince(DateTime thresholdTimestamp)
        {
            if (!_timeline.Any()) return Enumerable.Empty<(DateTime, T)>();

            var persistentElements = new List<(DateTime, T)>();
            foreach (var (timestamp, elements) in _timeline)
            {
                if (timestamp > thresholdTimestamp)
                {
                    foreach (var element in elements)
                    {
                        persistentElements.Add((timestamp, element));
                    }
                }
            }

            return persistentElements;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            _timeline.Clear();
        }
    }
}