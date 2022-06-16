using System;
using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private static class WsiShared
    {
        private static bool _eglLoaded;

        public static void EnsureEglAvailable()
        {
#if !FULL_RELEASE
            if (_eglLoaded || !OperatingSystem.IsWindows())
                return;

            // On non-published builds (so, development), GLFW can't find libEGL.dll
            // because it'll be in runtimes/<rid>/native/ instead of next to the actual executable.
            // We manually preload the library here so that GLFW will find it when it does its thing.
            NativeLibrary.TryLoad(
                "libEGL.dll",
                typeof(Clyde).Assembly,
                DllImportSearchPath.SafeDirectories,
                out _);

            _eglLoaded = true;
#endif
        }
    }
}
