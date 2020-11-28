using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils.General
{
    internal sealed class DisposableCollection : IDisposable, ICollection<IDisposable>
    {
        readonly List<IDisposable> _self;

        public DisposableCollection()
        {
            _self = new List<IDisposable>();
        }

        public void Dispose()
        {
            foreach (var disposable in _self)
            {
                disposable.Dispose();
            }
        }

        public int Count => _self.Count;
        public bool IsReadOnly => false;

        public IEnumerator<IDisposable> GetEnumerator() => _self.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _self.GetEnumerator();
        public void Add(IDisposable item) => _self.Add(item);
        public void Clear() => Dispose();
        public bool Contains(IDisposable item) => _self.Contains(item);
        public void CopyTo(IDisposable[] array, int arrayIndex) => _self.CopyTo(array, arrayIndex);
        public bool Remove(IDisposable item) => _self.Remove(item);
    }
}