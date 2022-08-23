using System;
using System.Threading;

namespace Robust.Shared.Utility
{
    internal static class InterlockedHelper
    {
        // Based on https://devblogs.microsoft.com/oldnewthing/20040915-00/?p=37863
        public static void Min(ref uint a, uint b)
        {
            uint original;
            uint result;
            do
            {
                original = a;
                result = Math.Min(original, b);
            } while (Interlocked.CompareExchange(ref a, result, original) != original);
        }
    }
}
