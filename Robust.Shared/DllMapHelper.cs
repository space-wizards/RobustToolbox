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
                // This particular API is only really used by the MIDI instruments stuff in SS14 right now,
                //  which means when it breaks people don't notice or report.
                if (name != baseName)
                {
                    return IntPtr.Zero;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return NativeLibrary.Load(linuxName, assembly, path);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return NativeLibrary.Load(macName, assembly, path);
                }

                return IntPtr.Zero;
            });
#endif
        }
    }
}
