using System.Collections.Generic;

namespace TorchMonitor.Monitors
{
    public sealed class NameConflictSolver
    {
        readonly Dictionary<string, HashSet<long>> _self;

        public NameConflictSolver()
        {
            _self = new Dictionary<string, HashSet<long>>();
        }

        public string GetSafeName(string name, long id)
        {
            var ids = GetIdSetOfName(name);
            ids.Add(id);

            return ids.Count == 1 ? name : $"{name} ({id})";
        }

        ISet<long> GetIdSetOfName(string name)
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