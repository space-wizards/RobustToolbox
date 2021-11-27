using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Robust.Shared
{
    internal static class DllMapHelper
    {
        [Conditional("NETCOREAPP")]
        public static void RegisterSimpleMap(Assembly assembly, string baseName)
        {
            // On .NET Framework this doesn't need to run because:
            // On Windows, the DLL names should check out correctly to just work.
            // On Linux/macOS, Mono's DllMap handles it for us.
            RegisterExplicitMap(assembly, $"{baseName}.dll", $"lib{baseName}.so", $"lib{baseName}.dylib");
        }

        [Conditional("NETCOREAPP")]
        public static void RegisterExplicitMap(Assembly assembly, string baseName, string linuxName, string macName)
        {
            // On .NET Framework this doesn't need to run because:
            // On Windows, the DLL names should check out correctly to just work.
            // On Linux/macOS, Mono's DllMap handles it for us.
#if NETCOREAPP
            NativeLibrary.SetDllImportResolver(assembly, (name, assembly, path) =>
            {
                // Please keep in sync with what GLFWNative does.
                // This particular API isn't used by anything at all, but I really wish it was.
                if (name != baseName)
                {
                    return IntPtr.Zero;
                }

                string? rName = null;
                if (OperatingSystem.IsLinux()) rName = linuxName;
                if (OperatingSystem.IsMacOS()) rName = macName;

                if ((rName != null) && NativeLibrary.TryLoad(rName, assembly, path, out var handle))
                    return handle;

                return IntPtr.Zero;
            });
#endif
        }
    }
}
