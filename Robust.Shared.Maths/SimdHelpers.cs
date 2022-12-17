using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Helper stuff for SIMD code.
    /// </summary>
    internal static class SimdHelpers
    {
        /// <returns>The min value is broadcast to the whole vector.</returns>
        public static Vector128<float> MinHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = Vector128.Min(b, v);
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return Vector128.Min(c, m);
        }

        /// <returns>The max value is broadcast to the whole vector.</returns>
        public static Vector128<float> MaxHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = Vector128.Max(b, v);
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return Vector128.Max(c, m);
        }

        /// <returns>The added value is broadcast to the whole vector.</returns>
        public static Vector128<float> AddHorizontal128(Vector128<float> v)
        {
            var b = Vector128.Shuffle(v, Vector128.Create(1, 0, 3, 2));
            var m = Vector128.Add(b, v);
            var c = Vector128.Shuffle(m, Vector128.Create(2, 3, 0, 1));
            return Vector128.Add(c, m);
        }

        /// <returns>The added value is broadcast to the whole vector.</returns>
        public static Vector256<float> AddHorizontal256(Vector256<float> v)
        {
            var b = Vector256.Shuffle(v, Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6));
            var m = Vector256.Add(b, v);
            var c = Vector256.Shuffle(m, Vector256.Create(2, 3, 0, 1, 6, 7, 4, 5));
            var n = Vector256.Add(c, m);
            var d = Vector256.Shuffle(n, Vector256.Create(4, 5, 6, 7, 0, 1, 2, 3));
            return Vector256.Add(n, d);
        }
    }
}
