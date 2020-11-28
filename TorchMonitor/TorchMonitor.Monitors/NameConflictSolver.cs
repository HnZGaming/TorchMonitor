﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TorchUtils;

namespace TorchMonitor.Monitors
{
    public sealed class NameConflictSolver
    {
        readonly Dictionary<string, HashSet<long>> _self;

        public NameConflictSolver()
        {
            _self = new Dictionary<string, HashSet<long>>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetSafeName(string name, long id)
        {
            name.ThrowIfNullOrEmpty(nameof(name));
            var ids = GetIdsByName(name);
            ids.Add(id);

            return ids.Count == 1 ? name : $"{name} ({id})";
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        ISet<long> GetIdsByName(string name)
        {
            if (!_self.TryGetValue(name, out var ids))
            {
                ids = new HashSet<long>();
                _self[name] = ids;
            }

            return ids;
        }
    }
}