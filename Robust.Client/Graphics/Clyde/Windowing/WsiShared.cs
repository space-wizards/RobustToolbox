using System;
using System.Runtime.InteropServices;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using TerraFX.Interop.Windows;
using TerraFX.Interop.Xlib;
using X11Window = TerraFX.Interop.Xlib.Window;

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

        public static unsafe void WindowsSharedWindowCreate(HWND hWnd, IConfigurationManager cfg)
        {
            // TODO: REMOVE, only used by GLFW, SDL3 does DWMWA_USE_IMMERSIVE_DARK_MODE automatically.

            // >= Windows 11 22000 check
            if (cfg.GetCVar(CVars.DisplayWin11ImmersiveDarkMode) && Environment.OSVersion.Version.Build >= 22000)
            {
                var b = BOOL.TRUE;
                Windows.DwmSetWindowAttribute(hWnd, 20, &b, (uint) sizeof(BOOL));
            }
        }

        public static void SetWindowStyleNoTitleOptionsWindows(HWND hWnd)
        {
            DebugTools.Assert(hWnd != HWND.NULL);

            Windows.SetWindowLongPtrW(
                hWnd,
                GWL.GWL_STYLE,
                // Cast to long here to work around a bug in rider with nint bitwise operators.
                (nint)((long)Windows.GetWindowLongPtrW(hWnd, GWL.GWL_STYLE) & ~WS.WS_SYSMENU));
        }

        public static unsafe void SetWindowStyleNoTitleOptionsX11(Display* x11Display, X11Window x11Window)
        {
            DebugTools.Assert(x11Window != X11Window.NULL);
            // https://specifications.freedesktop.org/wm-spec/wm-spec-latest.html#idm46181547486832
            var newPropValString = Marshal.StringToCoTaskMemUTF8("_NET_WM_WINDOW_TYPE_DIALOG");
            var newPropVal = Xlib.XInternAtom(x11Display, (sbyte*)newPropValString, Xlib.False);
            DebugTools.Assert(newPropVal != Atom.NULL);

            var propNameString = Marshal.StringToCoTaskMemUTF8("_NET_WM_WINDOW_TYPE");
#pragma warning disable CA1806
            // [display] [window] [property] [type] [format (8, 16,32)] [mode] [data] [element count]
            Xlib.XChangeProperty(x11Display, x11Window,
                Xlib.XInternAtom(x11Display, (sbyte*)propNameString, Xlib.False), // should never be null; part of spec
                Xlib.XA_ATOM, 32, Xlib.PropModeReplace,
                (byte*)&newPropVal, 1);
#pragma warning restore CA1806

            Marshal.FreeCoTaskMem(newPropValString);
            Marshal.FreeCoTaskMem(propNameString);
        }
    }
}
