﻿using System;

namespace TorchMonitor.Utils
{
    public sealed class ActionDisposable : IDisposable
    {
        readonly Action _action;

        public ActionDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}