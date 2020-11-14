using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Intervals
{
    public sealed class IntervalRunner : IDisposable
    {
        public interface IConfig
        {
            bool EnableLog { get; }
        }

        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly int _intervalSeconds;
        readonly List<IIntervalListener> _listeners;
        readonly CancellationTokenSource _canceller;

        public IntervalRunner(IConfig config, int intervalSeconds)
        {
            _config = config;
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
            lock (_listeners)
            {
                _listeners.Add(listener);
            }
        }

        public void AddListeners(IEnumerable<IIntervalListener> listeners)
        {
            lock (_listeners)
            {
                _listeners.AddRange(listeners);
            }
        }

        public void RemoveListener(IIntervalListener listener)
        {
            lock (_listeners)
            {
                _listeners.Remove(listener);
            }
        }

        public void RunIntervals()
        {
            var intervalSinceStart = 0;

            while (!_canceller.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                var intervalsSinceStartCopy = intervalSinceStart; // closure
                lock (_listeners)
                {
                    Parallel.ForEach(_listeners, listener =>
                    {
                        try
                        {
                            listener.OnInterval(intervalsSinceStartCopy);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    });
                }

                intervalSinceStart += 1;

                var spentTime = (DateTime.UtcNow - startTime).TotalSeconds;
                if (spentTime > _intervalSeconds)
                {
                    Log.Warn($"Spent more than 1 second: {spentTime}s");
                    continue;
                }

                var waitTime = _intervalSeconds - spentTime;
                _canceller.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(waitTime));

                if (_config.EnableLog)
                {
                    Log.Info($"interval: {intervalSinceStart}s");
                }
            }
        }
    }
}