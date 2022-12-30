using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Sub

        /// <summary>
        ///     Subtracts b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sub(Span<float> a, ReadOnlySpan<float> b)
        {
            Sub(a, b, a);
        }

        /// <summary>
        ///     Subtracts b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sub(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Sub256(a, b, s);
                return;
            }

            Sub128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SubScalar(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] - b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Sub128(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                    Vector128.Subtract(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                SubScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Sub256(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                    Vector256.Subtract(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                SubScalar(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region SubByScalar

        /// <summary>
        ///     Subtracts scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sub(Span<float> a, float b)
        {
            Sub(a, b, a);
        }

        /// <summary>
        ///     Subtracts scalar b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sub(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Sub256(a, b, s);
                return;
            }

            Sub128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SubScalar(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] - b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Sub128(ReadOnlySpan<float> a, float b, Span<float> s)
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

                    Vector128.Subtract(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                SubScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Sub256(ReadOnlySpan<float> a, float b, Span<float> s)
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

                    Vector256.Subtract(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                SubScalar(a, b, s, length, a.Length);
            }
        }

        #endregion
    }
}
