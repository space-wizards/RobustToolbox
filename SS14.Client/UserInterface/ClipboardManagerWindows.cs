using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;

namespace SS14.Client.UserInterface
{
    // Yay Windows API!
    sealed class ClipboardManagerWindows : IClipboardManager
    {
        [Dependency]
        private readonly IClyde _clyde;

        public bool Available => true;

        public string NotAvailableReason => "";

        public string GetText()
        {
            var windowHandle = _clyde.GetNativeWindowHandle();
            if (!OpenClipboard(windowHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                {
                    var dataHandle = GetClipboardData(CF_UNICODETEXT);
                    if (dataHandle == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    var ptr = GlobalLock(dataHandle);

                    if (ptr == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    try
                    {
                        var str = Marshal.PtrToStringUni(ptr);
                        return str;
                    }
                    finally
                    {
                        GlobalUnlock(dataHandle);
                    }
                }

                if (IsClipboardFormatAvailable(CF_TEXT))
                {
                    var dataHandle = GetClipboardData(CF_TEXT);
                    if (dataHandle == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    var ptr = GlobalLock(dataHandle);

                    if (ptr == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    try
                    {
                        var str = Marshal.PtrToStringAnsi(ptr);
                        return str;
                    }
                    finally
                    {
                        GlobalUnlock(dataHandle);
                    }
                }

                // Clipboard data isn't available as string, guess we just say it's empty.
                return "";
            }
            finally
            {
                CloseClipboard();
            }
        }

        public void SetText(string text)
        {
            void DoSetData(int allocLength, uint format, Func<string, IntPtr> allocHGlobal)
            {
                // Allocate global data for the clipboard contents.
                var alloc = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)allocLength);
                var clipboardSet = false;

                try
                {
                    // Copy data into global allocation.
                    var ptr = GlobalLock(alloc);
                    if (ptr == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    try
                    {
                        var uniPtr = allocHGlobal(text);
                        try
                        {
                            unsafe
                            {
                                Buffer.MemoryCopy((void*)uniPtr, (void*)ptr, allocLength, allocLength);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(uniPtr);
                        }
                    }
                    finally
                    {
                        GlobalUnlock(alloc);
                    }

                    // Set clipboard to global allocation.
                    var clip = SetClipboardData(format, alloc);
                    if (clip == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    clipboardSet = true;
                }
                finally
                {
                    if (!clipboardSet)
                    {
                        // If clipboardSet is false we didn't hand the data off to the clipboard and an error occured.
                        // In that case, try to avoid a memory leak by freeing the data.
                        GlobalFree(alloc);
                    }
                }
            }

            var windowHandle = _clyde.GetNativeWindowHandle();
            if (!OpenClipboard(windowHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            EmptyClipboard();

            try
            {
                DoSetData(text.Length * 2 + 1, CF_UNICODETEXT, Marshal.StringToHGlobalUni);
                DoSetData(text.Length + 1, CF_TEXT, Marshal.StringToHGlobalAnsi);
            }
            finally
            {
                CloseClipboard();
            }
        }

        private const uint CF_UNICODETEXT = 13;
        private const uint CF_TEXT = 1;
        private const uint GMEM_MOVEABLE = 2;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();
        
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);
    }
}
