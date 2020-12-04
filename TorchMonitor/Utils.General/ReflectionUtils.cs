using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Utils.General
{
    internal static class ReflectionUtils
    {
        public const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public static MethodInfo Method(this Type type, string name, BindingFlags flags)
        {
            return type.GetMethod(name, flags) ?? throw new Exception($"Couldn't find method {name} on {type}");
        }

        public static MethodInfo InstanceMethod(this Type t, string name)
        {
            return Method(t, name, InstanceFlags);
        }

        public static MethodInfo StaticMethod(this Type t, string name)
        {
            return Method(t, name, StaticFlags);
        }

        static Type[] GetTypesSafe(this Assembly self)
        {
            try
            {
                return self.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return new Type[0];
            }
        }

        // Type.GetType() but you don't have to specify the assembly qualified name (=durable to game updates)
        public static Type GetTypeByName(string fullName)
        {
            var typeName = fullName.Split('.').Last();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypesSafe())
            {
                if (type.Name != typeName) continue;
                if (!type.FullName?.Contains(fullName) ?? true) continue;

                return type;
            }

            throw new TypeInitializationException(fullName, new NullReferenceException());
        }

        public static Type[] GetDerivedTypes(this Type self)
        {
            var derivedTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypesSafe())
            {
                var isDerived = self.IsAssignableFrom(type);
                if (isDerived)
                {
                    derivedTypes.Add(type);
                }
            }

            return derivedTypes.ToArray();
        }

        public static bool TryGetAttribute<A>(this MemberInfo info, out A attribute) where A : Attribute
        {
            foreach (var customAttribute in info.GetCustomAttributes())
            {
                if (customAttribute.GetType() == typeof(A))
                {
                    attribute = (A) customAttribute;
                    return true;
                }
            }

            attribute = default;
            return false;
        }
    }
}