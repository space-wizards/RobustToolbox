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
            tmp = Sse.MoveScalar(max, min); // no generic Vector128 equivalent :(
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
            tmp = Sse.MoveScalar(max, min); // no generic Vector128 equivalent :(
            return Sse.Shuffle(tmp, tmp, 0b00_11_00_11);
        }

        /// <returns>The min value is broadcast to the whole vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> MinHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = Vector128.Min(b, v);
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return Vector128.Min(c, m);
        }

        /// <returns>The max value is broadcast to the whole vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> MaxHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = Vector128.Max(b, v);
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return Vector128.Max(c, m);
        }

        /// <returns>The added value is broadcast to the whole vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> AddHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = b + v;
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return c + m;
        }

        /// <returns>The added value is broadcast to the whole vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> AddHorizontal256(Vector256<float> v)
        {
            var b = Vector256.Shuffle(v, Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6));
            var m = b + v;
            var c = Vector256.Shuffle(m, Vector256.Create(2, 3, 0, 1, 6, 7, 4, 5));
            var n = c + m;
            var d = Vector256.Shuffle(n, Vector256.Create(4, 5, 6, 7, 0, 1, 2, 3));
            return n + d;
        }

        // Given the following vectors:
        // x:       X X X X
        // y:       Y Y Y Y
        // z:       Z Z Z Z
        // w:       W W W W
        // Returns: X Y Z W
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> MergeRows128(
            Vector128<float> x,
            Vector128<float> y,
            Vector128<float> z,
            Vector128<float> w)
        {
            if (Sse.IsSupported)
            {
                var xy = Sse.UnpackLow(x, y);
                var zw = Sse.UnpackLow(z, w);
                return Sse.Shuffle(xy, zw, 0b11_10_01_00);
            }

            return Vector128.Create(
                x.GetElement(0),
                y.GetElement(0),
                z.GetElement(0),
                w.GetElement(0));
        }
    }
}
