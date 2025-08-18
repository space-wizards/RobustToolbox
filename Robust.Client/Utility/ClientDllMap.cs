#if !WINDOWS
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Robust.Client.Utility
{
    internal static class ClientDllMap
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NativeLibrary.SetDllImportResolver(typeof(ClientDllMap).Assembly, (name, assembly, path) =>
            {
                if (name == "swnfd.dll")
                {
#if LINUX || FREEBSD
                    return NativeLibrary.Load("libswnfd.so", assembly, path);
#elif MACOS
                    return NativeLibrary.Load("libswnfd.dylib", assembly, path);
#endif
                }

                if (name == "libEGL.dll")
                {
#if LINUX || FREEBSD
                    return NativeLibrary.Load("libEGL.so", assembly, path);
#endif
                }

                if (name == SDL.nativeLibName)
                {
#if LINUX || FREEBSD
                    return NativeLibrary.Load("libSDL3.so.0", assembly, path);
#elif MACOS
                    return NativeLibrary.Load("libSDL3.0.dylib", assembly, path);
#endif
                }

                return IntPtr.Zero;
            });
        }
    }
}
#endif
