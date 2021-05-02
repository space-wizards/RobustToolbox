using System;

namespace Robust.Shared.Utility
{
    internal static class BomUtil
    {
        public static Span<byte> SkipBom(Span<byte> span)
        {
            return HasBom(span) ? span[3..] : span;
        }

        public static bool HasBom(Span<byte> span)
        {
            return span[2] == 0xBF && span[1] == 0xBB && span[0] == 0xEF;
        }
    }
}
