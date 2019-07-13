using System;

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

        // This can be replaced with CoreFX's version if we ever decide to drop .NET Framework.
        // For now we're stuck with it, though.
        public static unsafe string PtrToStringUTF8(byte* ptr)
        {
            if (ptr == null)
            {
                return null;
            }

            var length = FindNullTerminator(ptr);

            return EncodingHelpers.UTF8.GetString(ptr, length);
        }
    }
}
