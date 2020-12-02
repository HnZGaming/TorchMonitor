using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Utils.General
{
    internal sealed class DeprecationObserver<T>
    {
        readonly ConcurrentDictionary<T, DateTime> _items;

        public DeprecationObserver()
        {
            _items = new ConcurrentDictionary<T, DateTime>();
        }

        public void Add(T item)
        {
            _items[item] = DateTime.UtcNow;
        }

        public IEnumerable<T> RemoveDeprecated(TimeSpan lifespan)
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
                _items.Remove(removedItem);
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