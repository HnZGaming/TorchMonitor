using System;
using System.Collections.Concurrent;
using NLog;

namespace Utils.General
{
    internal sealed class ActionQueue
    {
        readonly ConcurrentQueue<Action> _queue;

        public ActionQueue()
        {
            _queue = new ConcurrentQueue<Action>();
        }

        public void Add(Action action)
        {
            _queue.Enqueue(action);
        }

        public void Flush(ILogger logger)
        {
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
        }
    }
}