using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Add

        /// <summary>
        ///     Adds b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(Span<float> a, ReadOnlySpan<float> b)
        {
            Add(a, b, a);
        }

        /// <summary>
        ///     Adds b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Add256(a, b, s);
                return;
            }

            Add128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddScalar(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] + b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add128(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            var remainder = a.Length & (Vector128<float>.Count - 1);
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            fixed (float* ptrB = b)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector128<float>.Count)
                {
                    var j = Vector128.Load(ptr + i);
                    var k = Vector128.Load(ptrB + i);

                    Vector128.Add(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AddScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add256(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            var remainder = a.Length & (Vector256<float>.Count - 1);
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            fixed (float* ptrB = b)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector256<float>.Count)
                {
                    var j = Vector256.Load(ptr + i);
                    var k = Vector256.Load(ptrB + i);

                    Vector256.Add(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AddScalar(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region AddByScalar

        /// <summary>
        ///     Adds scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(Span<float> a, float b)
        {
            Add(a, b, a);
        }

        /// <summary>
        ///     Adds scalar b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Add256(a, b, s);
                return;
            }

            Add128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddScalar(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] + b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add128(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            var remainder = a.Length & (Vector128<float>.Count - 1);
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector128<float>.Count)
                {
                    var j = Vector128.Load(ptr + i);

                    Vector128.Add(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AddScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add256(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            var remainder = a.Length & (Vector256<float>.Count - 1);
            var length = a.Length - remainder;

            var scalar = Vector256.Create(b);

            fixed (float* ptr = a)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector256<float>.Count)
                {
                    var j = Vector256.Load(ptr + i);

                    Vector256.Add(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AddScalar(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region HorizontalAdd

        /// <summary>
        ///     Adds all elements of a and returns the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float HorizontalAdd(ReadOnlySpan<float> a)
        {
            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                return HorizontalAdd256(a);
            }

            return HorizontalAdd128(a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float HorizontalAddScalar(ReadOnlySpan<float> a, int start, int end)
        {
            var sum = 0f;

            for (var i = start; i < end; i++)
            {
                sum += a[i];
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float HorizontalAdd128(ReadOnlySpan<float> a)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var accumulator = Vector128.Create(0f);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Vector128.Load(ptr + i);
                    accumulator = Vector128.Add(accumulator, j);
                }
            }

            var sum = SimdHelpers.AddHorizontal128(accumulator).GetElement(0);

            if (remainder != 0)
            {
                sum += HorizontalAddScalar(a, length, a.Length);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float HorizontalAdd256(ReadOnlySpan<float> a)
        {
            var remainder = a.Length & 7;
            var length = a.Length - remainder;

            var accumulator = Vector256.Create(0f);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 8)
                {
                    var j = Vector256.Load(ptr + i);
                    accumulator = Vector256.Add(j, accumulator);
                }
            }

            var sum = SimdHelpers.AddHorizontal256(accumulator).GetElement(0);

            if (remainder != 0)
            {
                sum += HorizontalAddScalar(a, length, a.Length);
            }

            return sum;
        }

        #endregion
    }
}
