using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Utility
{
    internal static class MarshalHelper
    {
        public static unsafe int FindNullTerminator(byte* ptr)
        {
            if (ptr == null)
            {
                throw new ArgumentNullException(nameof(ptr));
            }

            var i = 0;
            while (*(ptr + i) != 0)
            {
                i++;
            }

            return i;
        }

        public static unsafe string? PtrToStringUTF8(byte* ptr)
        {
#if NETCOREAPP
            return Marshal.PtrToStringUTF8((IntPtr) ptr);
#else
            if (ptr == null)
            {
                return null;
            }

            var length = FindNullTerminator(ptr);

            return EncodingHelpers.UTF8.GetString(ptr, length);
#endif
        }

        public static unsafe IntPtr StringToCoTaskMemUTF8(string? str)
        {
#if NETCOREAPP

            return Marshal.StringToCoTaskMemUTF8(str);

#else
            if (str == null)
            {
                return IntPtr.Zero;
            }

            var maxByteLength = System.Text.Encoding.UTF8.GetMaxByteCount(str.Length);

            var ptr = (byte*) Marshal.AllocCoTaskMem(maxByteLength + 1);

            int actualLen;
            fixed (char* pChar = str)
            {
                actualLen = System.Text.Encoding.UTF8.GetBytes(pChar, str.Length, ptr, maxByteLength);
            }

            ptr[actualLen] = 0;

            return (IntPtr) ptr;
#endif
        }
    }
}
