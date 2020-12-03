using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Utils.General
{
    /// <summary>
    /// Typical object pool with thread-safe pooling/unpooling functions.
    /// O(1) for pooling and unpooling.
    /// </summary>
    /// <typeparam name="T">Type of objects to be stored in this pool.</typeparam>
    internal abstract class ObjectPool<T>
    {
        readonly ConcurrentBag<T> _pooledObjects;

        protected ObjectPool()
        {
            _pooledObjects = new ConcurrentBag<T>();
        }

        /// <summary>
        /// Create a new object. Called when unpooling is requested when the pool is empty.
        /// </summary>
        /// <returns>New object.</returns>
        protected abstract T CreateNew();

        /// <summary>
        /// Initialize given object. Called when an object is pooled.
        /// </summary>
        /// <param name="obj"></param>
        protected abstract void Reset(T obj);

        /// <summary>
        /// Unpool an object.
        /// </summary>
        /// <remarks>New object will be instantiated if no objects are present in the pool.</remarks>
        /// <returns>An unpooled object or new object if the pool is empty.</returns>
        public T UnpoolOrCreate()
        {
            if (!_pooledObjects.TryTake(out var pooledObject))
            {
                return CreateNew();
            }

            return pooledObject;
        }

        /// <summary>
        /// Pool an object.
        /// </summary>
        /// <param name="obj">Object to pool.</param>
        public void Pool(T obj)
        {
            Reset(obj);
            _pooledObjects.Add(obj);
        }

        /// <summary>
        /// Pool all objects in given collection.
        /// </summary>
        /// <param name="objects">Collection of objects to be pooled.</param>
        public void PoolAll(IEnumerable<T> objects)
        {
            foreach (var obj in objects)
            {
                Pool(obj);
            }
        }
    }
}