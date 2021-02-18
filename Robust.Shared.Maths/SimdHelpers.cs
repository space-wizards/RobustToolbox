using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Helper stuff for SIMD code.
    /// </summary>
    internal static class SimdHelpers
    {
        /// <returns>The min value is broadcast to the whole vector.</returns>
        public static Vector128<float> MinHorizontalSse(Vector128<float> v)
        {
            var b = Sse.Shuffle(v, v, 0b10_11_00_01);
            var m = Sse.Min(b, v);
            var c = Sse.Shuffle(m, m, 0b01_00_11_10);
            return Sse.Min(c, m);
        }

        /// <returns>The max value is broadcast to the whole vector.</returns>
        public static Vector128<float> MaxHorizontalSse(Vector128<float> v)
        {
            var b = Sse.Shuffle(v, v, 0b10_11_00_01);
            var m = Sse.Max(b, v);
            var c = Sse.Shuffle(m, m, 0b01_00_11_10);
            return Sse.Max(c, m);
        }
    }
}
