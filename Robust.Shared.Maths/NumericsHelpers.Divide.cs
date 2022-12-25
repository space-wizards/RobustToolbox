using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Divide

        /// <summary>
        ///     Divide a by b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(Span<float> a, ReadOnlySpan<float> b)
        {
            Divide(a, b, a);
        }

        /// <summary>
        ///     Divide a by b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Divide256(a, b, s);
                return;
            }

            Divide128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DivideScalar(
            ReadOnlySpan<float> a,
            ReadOnlySpan<float> b,
            Span<float> s,
            int start,
            int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] / b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Divide128(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                    Vector128.Divide(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                DivideScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Divide256(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                    Vector256.Divide(j, k).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                DivideScalar(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region DivideByScalar

        /// <summary>
        ///     Divide a by scalar b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(Span<float> a, float b)
        {
            Divide(a, b, a);
        }

        /// <summary>
        ///     Divide a by scalar b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Divide256(a, b, s);
                return;
            }

            Divide128(a, b, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DivideScalar(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] / b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Divide128(ReadOnlySpan<float> a, float b, Span<float> s)
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

                    Vector128.Divide(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                DivideScalar(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Divide256(ReadOnlySpan<float> a, float b, Span<float> s)
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

                    Vector256.Divide(j, scalar).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                DivideScalar(a, b, s, length, a.Length);
            }
        }

        #endregion
    }
}
