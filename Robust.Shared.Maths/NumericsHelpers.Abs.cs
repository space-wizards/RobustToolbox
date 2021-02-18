using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Abs

        /// <summary>
        ///     Does abs on a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Abs(Span<float> a)
        {
            Abs(a, a);
        }

        /// <summary>
        ///     Does abs on a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Abs(ReadOnlySpan<float> a, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && Avx2.IsSupported && LengthValid256Single(a.Length))
                {
                    AbsAvx(a, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported && Sse2.IsSupported)
                    {
                        AbsSse(a, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        AbsAdvSimd(a, s);
                        return;
                    }
                }
            }

            AbsNaive(a, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsNaive(ReadOnlySpan<float> a, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Abs(a[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsSse(ReadOnlySpan<float> a, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var mask = Sse2.ShiftRightLogical(Vector128.Create(-1), 1).AsSingle();

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);

                        Sse.Store(ptrS + i, Sse.And(mask, j));
                    }
                }
            }

            if(remainder != 0)
            {
                AbsNaive(a, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsAdvSimd(ReadOnlySpan<float> a, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = AdvSimd.LoadVector128(ptr + i);

                        AdvSimd.Store(ptrS + i, AdvSimd.Abs(j));
                    }
                }
            }

            if(remainder != 0)
            {
                AbsNaive(a, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsAvx(ReadOnlySpan<float> a, Span<float> s)
        {
            var remainder = a.Length & 7;
            var length = a.Length - remainder;

            var mask = Avx2.ShiftRightLogical(Vector256.Create(-1), 1).AsSingle();

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 8)
                    {
                        var j = Avx.LoadVector256(ptr + i);

                        Avx.Store(ptrS + i, Avx.And(mask, j));
                    }
                }
            }

            if(remainder != 0)
            {
                AbsNaive(a, s, length, a.Length);
            }
        }

        #endregion
    }
}
