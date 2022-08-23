using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Client.Utility
{
    internal static class ClientDllMap
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            if (OperatingSystem.IsWindows())
                return;

            NativeLibrary.SetDllImportResolver(typeof(ClientDllMap).Assembly, (name, assembly, path) =>
            {
                if (name == "swnfd.dll")
                {
                    if (OperatingSystem.IsLinux())
                        return NativeLibrary.Load("libswnfd.so", assembly, path);

                    if (OperatingSystem.IsMacOS())
                        return NativeLibrary.Load("libswnfd.dylib", assembly, path);

                    return IntPtr.Zero;
                }

                if (name == "libEGL.dll")
                {
                    if (OperatingSystem.IsLinux())
                        return NativeLibrary.Load("libEGL.so", assembly, path);

                    return IntPtr.Zero;
                }

                return IntPtr.Zero;
            });
        }
    }
}
