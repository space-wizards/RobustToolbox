using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Helper stuff for SIMD code.
    /// </summary>
    internal static class SimdHelpers
    {
        /// <returns>A vector with the horizontal minimum and maximum values arranged as { min max min max} .</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> MinMaxHorizontalSse(Vector128<float> input)
        {
            var tmp = Sse.Shuffle(input, input, 0b00_01_10_11);
            var min = Sse.Min(tmp, input);
            var max = Sse.Max(tmp, input);
            tmp = Sse.Shuffle(min, max, 0b01_00_00_01);
            min = Sse.Min(tmp, min);
            max = Sse.Max(tmp, max);
            tmp = Sse.MoveScalar(max, min);
            return Sse.Shuffle(tmp, tmp, 0b11_00_11_00);
        }

        /// <returns>A vector with the horizontal minimum and maximum values arranged as { max min max min} .</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> MaxMinHorizontalSse(Vector128<float> input)
        {
            var tmp = Sse.Shuffle(input, input, 0b00_01_10_11);
            var min = Sse.Min(tmp, input);
            var max = Sse.Max(tmp, input);
            tmp = Sse.Shuffle(min, max, 0b01_00_00_01);
            min = Sse.Min(tmp, min);
            max = Sse.Max(tmp, max);
            tmp = Sse.MoveScalar(max, min);
            return Sse.Shuffle(tmp, tmp, 0b00_11_00_11);
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
