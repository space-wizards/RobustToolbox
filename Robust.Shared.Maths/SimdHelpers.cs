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

        #region GetAABB

        /// <summary>
        /// This computes the bounding box given a set of 4 coordinates specified via 2 simd vectors.
        /// This effectively computes the horizontal min & max of both of the given vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> GetAABB(Vector128<float> x, Vector128<float> y)
        {
            if (!Avx.IsSupported)
                return GetAABBSlow(x, y);

            return GetAABB128(x, y);
        }

        /// <summary>
        /// This computes the bounding box given a set of 4 coordinates specified via 2 simd vectors.
        /// This effectively computes the horizontal min & max of both of the given vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> GetAABB128(Vector128<float> x, Vector128<float> y)
        {
            // x = [x0, x1, x2, x3]
            // y = [y0, y1, y2, y3]

            var xmin = Vector128.Shuffle(x, Vector128.Create(1, 0, 3, 2));
            xmin = Sse.Min(xmin, x);
            // xmin = [min(x0,x1), min(x0,x1), min(x2,x3), min(x2,x3)]

            var ymin = Vector128.Shuffle(y, Vector128.Create(1, 0, 3, 2));
            ymin = Sse.Min(ymin, y);
            // ymin = [min(y0,y1), min(y0,x1), min(y2,y3), min(y2,y3)]

            var xymin = Sse41.Blend(xmin, ymin, 0b_1_0_1_0);
            // xymin = [min(x0,x1), min(y0,y1), min(x2,x3), min(y2,y3)]

            var xyminPermuted = Avx.Permute(xymin, 0b_00_00_11_10);
            // xymin_permuted = [min(x2,x3), min(y2,y3), ..., ... ]

            var min = Sse.Min(xymin, xyminPermuted);
            // min = [min(x0,x1,x2,x3), min(y0,y1,y2,y3), ..., ... ]

            var xmax = Vector128.Shuffle(x, Vector128.Create(1, 0, 3, 2));
            xmax = Sse.Max(xmax, x);
            // xmax = [max(x0,x1), max(x0,x1), max(x2,x3), max(x2,x3)]

            var ymax = Vector128.Shuffle(y, Vector128.Create(1, 0, 3, 2));
            ymax = Sse.Max(ymax, y);
            // ymax = [max(y0,y1), max(y0,y1), max(y2,y3), max(y2,y3)]

            var xymax = Sse41.Blend(xmax, ymax, 0b_1_0_1_0);
            // xymax = [max(x0,x1), max(y0,y1), max(x2,x3), max(y2,y3)]

            var xymaxPermuted = Avx.Permute(xymax, 0b_01_00_00_00);
            // xymax_permuted = [.., .., max(x0,x1), max(y0,y1) ]

            var max = Sse.Max(xymax, xymaxPermuted);
            // max = [.., .., max(x0,x1,x2,x3), max(y0,y1,y2,y3) ]

            // result = [min(x0,x1,x2,x3), min(y0,y1,y2,y3), max(x0,x1,x2,x3), max(y0,y1,y2,y3) ]
            return Sse41.Blend(min, max, 0b_1_1_0_0);
        }

        /// <summary>
        /// This computes the bounding box given a set of 4 coordinates specified via 2 simd vectors.
        /// This effectively computes the horizontal min & max of both of the given vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> GetAABB256(Vector128<float> x, Vector128<float> y)
        {
            var xy = Vector256.Create(x, y);
            // xy = [x0, x1, x2, x3, y0, y1, y2, y3]

            var xyPermuted = Avx.Permute(xy, 0b_10_11_00_01);
            // xy_permuted = [x1, x0, x3, x2, y1, y0, y3, y2]

            var min = Avx.Min(xy, xyPermuted);
            var max = Avx.Max(xy, xyPermuted);
            // min = [min(x0,x1), min(x0,x1), min(x2,x3), min(x2,x3), min(y0,y1), min(y0,x1), min(y2,y3), min(y2,y3)]

            var minPermuted = Avx.Permute(min, 0b_00_00_10_10);
            var maxPermuted = Avx.Permute(max, 0b_00_00_10_10);
            // min_permuted = [min(x2,x3), min(x2,x3), min(x0,x1), min(x0,x1), min(y2,y3), min(y2,y3), min(y0,y1), min(y0,x1)]

            min = Avx.Min(min, minPermuted);
            max = Avx.Max(max, maxPermuted);
            // min = [min(x0,x1,x2,x3), min(x0,x1,x2,x3), min(x0,x1,x2,x3), min(x0,x1,x2,x3), min(y0,y1,y2,y3), min(y0,y1,y2,y3), min(y0,y1,y2,y3), min(y0,y1,y2,y3)]

            var minFlipped = Avx.Permute2x128(min, min, 0b_00_01);
            var maxFlipped = Avx.Permute2x128(max, max, 0b_00_01);
            // flip upper & lower 128 bits
            // min_flipped = [min(y0,y1,y2,y3), min(y0,y1,y2,y3), min(y0,y1,y2,y3), min(y0,y1,y2,y3), min(x0,x1,x2,x3), min(x0,x1,x2,x3), min(x0,x1,x2,x3), min(x0,x1,x2,x3)]

            min = Avx.Blend(min, minFlipped, 0b_00_00_10_10);
            max = Avx.Blend(max, maxFlipped, 0b_00_00_10_10);
            // min_flipped = [min(x0,x1,x2,x3), min(y0,y1,y2,y3), min(x0,x1,x2,x3), min(y0,y1,y2,y3), ...]

            return Avx.Blend(min, max, 0b_00_00_11_00).GetLower();
            // [min(x0,x1,x2,x3), min(y0,y1,y2,y3), max(x0,x1,x2,x3), max(y0,y1,y2,y3)]
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> GetAABBSlow(Vector128<float> x, Vector128<float> y)
        {
            var l = MinHorizontal128(x);
            var b = MinHorizontal128(y);
            var r = MaxHorizontal128(x);
            var t = MaxHorizontal128(y);
            return MergeRows128(l, b, r, t);
        }

        #endregion

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
