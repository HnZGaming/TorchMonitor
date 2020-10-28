﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TorchMonitor.Business
{
    public sealed class IntervalRunner : IDisposable
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        readonly int _intervalSeconds;
        readonly List<IIntervalListener> _listeners;
        readonly CancellationTokenSource _canceller;

        public IntervalRunner(int intervalSeconds)
        {
            _intervalSeconds = intervalSeconds;
            _listeners = new List<IIntervalListener>();
            _canceller = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _canceller.Cancel();
            _canceller.Dispose();
        }

        public void AddListener(IIntervalListener listener)
        {
            _listeners.Add(listener);
        }

        public void AddListeners(IEnumerable<IIntervalListener> listeners)
        {
            _listeners.AddRange(listeners);
        }

        public void RemoveListener(IIntervalListener listener)
        {
            _listeners.Remove(listener);
        }

        public void RunIntervals()
        {
            var intervalSinceStart = 0;

            while (!_canceller.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                var intervalsSinceStartCopy = intervalSinceStart; // closure
                Parallel.ForEach(_listeners, monitor =>
                {
                    try
                    {
                        monitor.OnInterval(intervalsSinceStartCopy);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });

                intervalSinceStart += 1;

                var spentTime = (DateTime.UtcNow - startTime).TotalSeconds;
                if (spentTime > _intervalSeconds)
                {
                    Log.Warn($"Spent more than 1 second: {spentTime}s");
                    continue;
                }

                var waitTime = _intervalSeconds - spentTime;
                _canceller.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(waitTime));
            }
        }
    }
}