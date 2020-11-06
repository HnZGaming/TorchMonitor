﻿using System;
using System.Collections.Generic;

namespace TorchUtils
{
    internal static class DisposableUtils
    {
        public static T AddedTo<T>(this T self, ICollection<IDisposable> bag) where T : IDisposable
        {
            bag.Add(self);
            return self;
        }
    }
}