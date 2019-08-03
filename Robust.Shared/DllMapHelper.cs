using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Robust.Shared
{
    internal static class DllMapHelper
    {
        [Conditional("NETCOREAPP")]
        public static void RegisterSimpleMap(Assembly assembly, string baseName)
        {
#if NETCOREAPP
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // DLL names should line up on Windows by default.
                // So a hook won't do anything.
                return;
            }

            NativeLibrary.SetDllImportResolver(assembly, (name, _, __) =>
            {
                if (name == baseName)
                {
                    var assemblyDir = Path.GetDirectoryName(assembly.Location);

                    string libName;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        libName = $"lib{baseName}.so";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        libName = $"lib{baseName}.dylib";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return NativeLibrary.Load(Path.Join(assemblyDir, libName));
                }

                return IntPtr.Zero;
            });
#endif
        }

        [Conditional("NETCOREAPP")]
        public static void RegisterExplicitMap(Assembly assembly, string baseName, string linuxName, string macName)
        {
#if NETCOREAPP
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // DLL names should line up on Windows by default.
                // So a hook won't do anything.
                return;
            }

            NativeLibrary.SetDllImportResolver(assembly, (name, _, __) =>
            {
                if (name == baseName)
                {
                    string libName;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        libName = linuxName;
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        libName = macName;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return NativeLibrary.Load(libName);
                }

                return IntPtr.Zero;
            });
#endif
        }
    }
}
