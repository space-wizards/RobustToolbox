using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Robust.Client.Utility
{
    /// <summary>
    /// P/Invoke definitions for win32.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static unsafe class Win32
    {
        public const uint MB_ICONERROR = (uint) 0x00000010L;
        public const uint MB_OK = (uint) 0x00000000L;

        [DllImport("user32.dll")]
        public static extern int MessageBoxW(
            void* hWnd,
            [MarshalAs(UnmanagedType.LPWStr)] string lpText,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCaption,
            uint uType);

        public const int GWL_STYLE = -16;
        public const int GWLP_HWNDPARENT = -8;
        public const int WS_SYSMENU = 0x80000;

        public const int MF_BYCOMMAND = 0x00000000;
        public const int MF_DISABLED = 0x00000002;
        public const int MF_ENABLED = 0x00000000;
        public const int MF_GRAYED = 0x00000001;

        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint GetWindowLongPtrW(void* hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint SetWindowLongPtrW(void* hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll")]
        public static extern void* GetSystemMenu(void* hWnd, int bRevert);

        [DllImport("user32.dll")]
        public static extern int EnableMenuItem(void* hMenu, uint uIDEnableItem, uint uEnable);
    }
}

