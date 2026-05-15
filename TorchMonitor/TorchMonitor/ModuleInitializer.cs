using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TorchMonitor
{
    static class ModuleInitializer
    {
        // Runs as a .NET module initializer — immediately when TorchMonitor.dll is
        // loaded into the AppDomain, before Assembly.GetTypes() or any static
        // constructor is called. This is the only place early enough to redirect
        // the Profiler assembly request before Torch's PatchManager calls GetTypes().
        [ModuleInitializer]
        internal static void Init()
        {
            var dependencies = new HashSet<string>(new[]
            {
                "Profiler",
                "InfluxDb.Torch",
                "InfluxDb.Client",
            }, StringComparer.OrdinalIgnoreCase);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var requestedName = new AssemblyName(args.Name).Name;
                if (!dependencies.Contains(requestedName)) return null;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    if (string.Equals(asm.GetName().Name, requestedName, StringComparison.OrdinalIgnoreCase))
                        return asm;

                return null;
            };
        }
    }
}