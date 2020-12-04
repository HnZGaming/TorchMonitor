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

        public IEnumerable<T> Items => _items.Keys;

        public void Add(T item)
        {
            _items[item] = DateTime.UtcNow;
        }

        public IEnumerable<T> RemoveDeprecated(TimeSpan lifespan)
        {
            var endTime = DateTime.UtcNow - lifespan;

            var removedItems = new List<T>();
            foreach (var ti in _items)
            {
                var item = ti.Key;
                var timestamp = ti.Value;

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