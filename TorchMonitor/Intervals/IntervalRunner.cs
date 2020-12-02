using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Utils.General;

namespace Intervals
{
    public sealed class IntervalRunner
    {
        public interface IConfig
        {
            bool Enabled { get; }
        }

        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly int _intervalSeconds;
        readonly List<IIntervalListener> _listeners;

        public IntervalRunner(IConfig config, int intervalSeconds)
        {
            _config = config;
            _intervalSeconds = intervalSeconds;
            _listeners = new List<IIntervalListener>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddListeners(IEnumerable<IIntervalListener> listeners)
        {
            _listeners.AddRange(listeners);
        }

        public void LoopIntervals(CancellationToken _canceller)
        {
            var intervalSinceStart = 0;

            while (!_canceller.IsCancellationRequested)
            {
                if (!_config.Enabled)
                {
                    if (!_canceller.WaitHandle.WaitOneSafe(1.Seconds())) return;
                    continue;
                }

                var startTime = DateTime.UtcNow;

                RunIntervalOnce(intervalSinceStart);
                intervalSinceStart += 1;

                var spentTime = (DateTime.UtcNow - startTime).TotalSeconds;
                if (spentTime > _intervalSeconds)
                {
                    Log.Warn($"Spent more than 1 second: {spentTime}s");
                    continue;
                }

                var waitTime = _intervalSeconds - spentTime;
                _canceller.WaitHandle.WaitOneSafe(waitTime.Seconds());

                Log.Debug($"interval: {intervalSinceStart}s");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void RunIntervalOnce(int currentInterval)
        {
            Parallel.ForEach(_listeners, listener =>
            {
                try
                {
                    var startTime = DateTime.UtcNow;

                    listener.OnInterval(currentInterval);

                    var time = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    Log.Debug($"listener finished interval: \"{listener.GetType().Name}\", {time:0.000}ms");
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
        }
    }
}