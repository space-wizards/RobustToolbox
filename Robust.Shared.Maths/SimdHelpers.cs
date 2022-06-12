using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Helper stuff for SIMD code.
    /// </summary>
    public static class SimdHelpers
    {
        /// <returns>A vector with the horizontal minimum and maximum values arranged as { min max max min} .</returns>
        public static Vector128<float> MinMaxHorizontalSse(Vector128<float> v)
        {
            var b = Sse.Shuffle(v, v, 0b00_01_10_11);
            var m = Sse.Min(b, v);
            var M = Sse.Max(b, v);
            var c = Sse.Shuffle(m, M, 0b01_00_00_01);
            var mm = Sse.Min(c, m);
            var MM = Sse.Max(c, M);
            var d = Sse.MoveScalar(MM, mm);
            return Sse.Shuffle(d, d, 0b00_11_11_00);
        }

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
