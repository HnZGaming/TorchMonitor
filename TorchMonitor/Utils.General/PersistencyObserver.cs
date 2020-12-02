using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    internal sealed class PersistencyObserver<T>
    {
        readonly LinkedList<HashSet<T>> _timeline;

        public PersistencyObserver()
        {
            _timeline = new LinkedList<HashSet<T>>();
        }

        public void Clear()
        {
            _timeline.Clear();
        }

        public void CapBufferSize(int bufferSize)
        {
            while (_timeline.Count > bufferSize)
            {
                _timeline.RemoveLast();
            }
        }

        public void AddInterval(IEnumerable<T> items)
        {
            _timeline.AddFirst(new HashSet<T>(items));
        }

        public IEnumerable<T> GetElementsPresentInAllIntervals()
        {
            if (!_timeline.Any()) return new T[0];

            var firstItems = _timeline.First.Value;
            var persistentItems = new HashSet<T>(firstItems);
            foreach (var frame in _timeline)
            {
                persistentItems.IntersectWith(frame);
            }

            return persistentItems;
        }
    }
}