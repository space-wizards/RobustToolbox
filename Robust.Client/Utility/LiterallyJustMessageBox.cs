using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Robust.Client.Utility
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class LiterallyJustMessageBox
    {
        public const uint MB_ICONERROR = (uint) 0x00000010L;
        public const uint MB_OK = (uint) 0x00000000L;

        [DllImport("user32.dll")]
        public static extern unsafe int MessageBoxW(
            void* hWnd,
            [MarshalAs(UnmanagedType.LPWStr)] string lpText,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCaption,
            uint uType);
    }
}
