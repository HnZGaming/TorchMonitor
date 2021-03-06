﻿using System;
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddListener(IIntervalListener listener)
        {
            _listeners.Add(listener);
        }

        public async Task LoopIntervals(CancellationToken canceller)
        {
            var intervalSinceStart = 0;

            while (!canceller.IsCancellationRequested)
            {
                if (!_config.Enabled)
                {
                    await Task.Delay(1.Seconds(), canceller);
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
                await Task.Delay(waitTime.Seconds(), canceller);

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