using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Utils.General
{
    internal sealed class PersistencyObserver<T>
    {
        readonly LinkedList<(DateTime Timestamp, HashSet<T> Elements)> _timeline;

        public PersistencyObserver()
        {
            _timeline = new LinkedList<(DateTime Timestamp, HashSet<T> Elements)>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddInterval(IEnumerable<T> items)
        {
            _timeline.AddFirst((DateTime.UtcNow, new HashSet<T>(items)));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CapBufferSize(TimeSpan bufferTimeSpan)
        {
            if (!_timeline.Any()) return;

            while (true)
            {
                var (timestamp, _) = _timeline.Last.Value;
                var pastTime = DateTime.UtcNow - timestamp;
                if (pastTime > bufferTimeSpan)
                {
                    _timeline.RemoveLast();
                }
                else
                {
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<T> GetPersistentIntervals()
        {
            if (!_timeline.Any()) return new T[0];

            var firstItems = _timeline.First.Value.Elements;
            var persistentItems = new HashSet<T>(firstItems);
            foreach (var frame in _timeline)
            {
                persistentItems.IntersectWith(frame.Elements);
            }

            return persistentItems;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            _timeline.Clear();
        }
    }
}