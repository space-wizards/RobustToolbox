using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Abs

        /// <summary>
        ///     Does abs on a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Abs(Span<float> a)
        {
            Abs(a, a);
        }

        /// <summary>
        ///     Does abs on a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Abs(ReadOnlySpan<float> a, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Vector256Enabled && LengthValid256Single(a.Length))
            {
                Abs256(a, s);
                return;
            }

            Abs128(a, s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AbsScalar(ReadOnlySpan<float> a, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Abs(a[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Abs128(ReadOnlySpan<float> a, Span<float> s)
        {
            var remainder = a.Length & (Vector128<float>.Count - 1);
            var length = a.Length - remainder;

            var mask = Vector128.Create(unchecked((uint)-1 >> 1)).AsSingle();

            fixed (float* ptr = a)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector128<float>.Count)
                {
                    var j = Vector128.Load(ptr + i);

                    Vector128.BitwiseAnd(mask, j).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AbsScalar(a, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Abs256(ReadOnlySpan<float> a, Span<float> s)
        {
            var remainder = a.Length & (Vector256<float>.Count - 1);
            var length = a.Length - remainder;

            var mask = Vector256.Create(unchecked((uint)-1 >> 1)).AsSingle();

            fixed (float* ptr = a)
            fixed (float* ptrS = s)
            {
                for (var i = 0; i < length; i += Vector256<float>.Count)
                {
                    var j = Vector256.Load(ptr + i);

                    Vector256.BitwiseAnd(mask, j).Store(ptrS + i);
                }
            }

            if (remainder != 0)
            {
                AbsScalar(a, s, length, a.Length);
            }
        }

        #endregion
    }
}
