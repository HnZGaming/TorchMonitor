using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Utils.General;

namespace TorchMonitor.Utils
{
    public sealed class NameConflictSolver<T>
    {
        readonly Dictionary<string, HashSet<T>> _self;

        public NameConflictSolver()
        {
            _self = new Dictionary<string, HashSet<T>>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetSafeName(string name, T id)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "<noname>";
            }

            var ids = GetIdsByName(name);
            ids.Add(id);

            if (ids.Count == 1) return name;

            return $"{name} ({id})";
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        ISet<T> GetIdsByName(string name)
        {
            if (!_self.TryGetValue(name, out var ids))
            {
                ids = new HashSet<T>();
                _self[name] = ids;
            }

            return ids;
        }
    }
}