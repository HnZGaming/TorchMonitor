using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Utils.General
{
    /// <summary>
    /// Holds onto queued elements until the next time interval.
    /// </summary>
    /// <typeparam name="T">Type of elements.</typeparam>
    internal sealed class ThreadSafeThrottle<T>
    {
        readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly List<T> _queuedElements;
        readonly TimeSpan _throttleInterval;
        readonly Action<IEnumerable<T>> _onFlush;

        CancellationTokenSource _cancellationTokenSource;

        public ThreadSafeThrottle(
            TimeSpan throttleInterval,
            Action<IEnumerable<T>> onFlush)
        {
            _throttleInterval = throttleInterval;
            _onFlush = onFlush;
            _queuedElements = new List<T>();
        }

        public void Add(T element)
        {
            lock (this)
            {
                _queuedElements.Add(element);
            }
        }

        public bool Start()
        {
            // make a cancellation token source (to "stop" later)
            CancellationToken canceller;
            lock (this)
            {
                // already running
                if (_cancellationTokenSource != null)
                {
                    return false;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                canceller = _cancellationTokenSource.Token;
            }

            TaskUtils.RunUntilCancelledAsync(LoopFlush, canceller).Forget(Log);

            return true;
        }

        async Task LoopFlush(CancellationToken canceller)
        {
            while (!canceller.IsCancellationRequested)
            {
                Flush();
                await Task.Delay(_throttleInterval, canceller);
            }
        }

        public void Flush()
        {
            lock (this)
            {
                _onFlush(_queuedElements);
                _queuedElements.Clear();
            }
        }

        public bool Stop()
        {
            lock (this)
            {
                if (_cancellationTokenSource == null)
                {
                    // not started (or already stopped)
                    return false;
                }

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                return true;
            }
        }
    }
}