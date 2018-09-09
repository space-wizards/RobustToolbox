using System;

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
    }
}
