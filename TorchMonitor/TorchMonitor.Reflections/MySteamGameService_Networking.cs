using System;
using System.Reflection;
using VRage.GameServices;
using VRage.Steam;

namespace TorchMonitor.Reflections
{
    public static class MySteamGameService_Networking
    {
        const string Name = "m_networking";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        static readonly Type Type = typeof(MySteamGameService);
        static readonly FieldInfo Field = Type.GetField(Name, Flags);

        public static IMyNetworking Value => (IMyNetworking) Field.GetValue(null);
    }
}