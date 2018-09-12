using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.Utility
{
    public static class TypeHelpers
    {
        // TODO: These heuristics might be better off checking the actual assembly
        // against loaded assemblies, instead of string comparison.

        public static bool IsServerSide(this Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            return assemblyName.IndexOf("Server", StringComparison.Ordinal) != -1;
        }

        public static bool IsClientSide(this Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            return assemblyName.IndexOf("Client", StringComparison.Ordinal) != -1;
        }

        public static bool IsSharedSide(this Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            return assemblyName.IndexOf("Shared", StringComparison.Ordinal) != -1;
        }

        /// <summary>
        ///     Returns absolutely all fields, privates, readonlies, and ones from parents.
        /// </summary>
        public static IEnumerable<FieldInfo> GetAllFields(this Type t)
        {
            // We need to fetch the entire class hierarchy and SelectMany(),
            // Because BindingFlags.FlattenHierarchy doesn't read privates,
            // Even when you pass BindingFlags.NonPublic.
            return GetClassHierarchy(t).SelectMany(p => p.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public));
        }

        /// <summary>
        ///     Returns absolutely all instance properties on a type. Inherited and private included.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type t)
        {
            return GetClassHierarchy(t).SelectMany(p => p.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public));
        }

        public static IEnumerable<Type> GetClassHierarchy(this Type t)
        {
            yield return t;

            while (t.BaseType != null)
            {
                t = t.BaseType;
                yield return t;
            }
        }
    }
}
