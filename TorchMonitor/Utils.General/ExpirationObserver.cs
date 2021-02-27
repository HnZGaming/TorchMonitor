using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    internal sealed class ExpirationObserver<T>
    {
        readonly ConcurrentDictionary<T, DateTime> _items;

        public ExpirationObserver()
        {
            _items = new ConcurrentDictionary<T, DateTime>();
        }

        public IEnumerable<T> AllItems => _items.Keys;

        public void Add(T item)
        {
            _items[item] = DateTime.UtcNow;
        }

        public IEnumerable<T> RemoveOlderThan(TimeSpan lifespan)
        {
            var endTime = DateTime.UtcNow - lifespan;

            var removedItems = new List<T>();
            foreach (var (item, timestamp) in _items)
            {
                if (timestamp < endTime)
                {
                    removedItems.Add(item);
                }
            }

            foreach (var removedItem in removedItems)
            {
                _items.TryRemove(removedItem, out _);
            }

            return removedItems;
        }

        public IEnumerable<T> RemoveAll()
        {
            var allItems = _items.Keys.ToArray();
            _items.Clear();
            return allItems;
        }
    }
}