using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Sub

        /// <summary>
        ///     Subtracts b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(Span<float> a, ReadOnlySpan<float> b)
        {
            Sub(a, b, a);
        }

        /// <summary>
        ///     Subtracts b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    SubAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        SubSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        SubAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            SubNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] - b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubSse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    fixed (float* ptrS = s)
                    {
                        for (var i = 0; i < length; i += 4)
                        {
                            var j = Sse.LoadVector128(ptr + i);
                            var k = Sse.LoadVector128(ptrB + i);

                            Sse.Store(ptrS + i, Sse.Subtract(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    fixed (float* ptrS = s)
                    {
                        for (var i = 0; i < length; i += 4)
                        {
                            var j = AdvSimd.LoadVector128(ptr + i);
                            var k = AdvSimd.LoadVector128(ptrB + i);

                            AdvSimd.Store(ptrS + i, AdvSimd.Subtract(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            var remainder = a.Length & 7;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    fixed (float* ptrS = s)
                    {
                        for (var i = 0; i < length; i += 8)
                        {
                            var j = Avx.LoadVector256(ptr + i);
                            var k = Avx.LoadVector256(ptrB + i);

                            Avx.Store(ptrS + i, Avx.Subtract(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region SubByScalar

        /// <summary>
        ///     Subtracts scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(Span<float> a, float b)
        {
            Sub(a, b, a);
        }

        /// <summary>
        ///     Subtracts scalar b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    SubAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        SubSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        SubAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            SubNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] - b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubSse(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);

                        Sse.Store(ptrS + i, Sse.Subtract(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = AdvSimd.LoadVector128(ptr + i);

                        AdvSimd.Store(ptrS + i, AdvSimd.Subtract(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubAvx(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            var remainder = a.Length & 7;
            var length = a.Length - remainder;

            var scalar = Vector256.Create(b);

            fixed (float* ptr = a)
            {
                fixed (float* ptrS = s)
                {
                    for (var i = 0; i < length; i += 8)
                    {
                        var j = Avx.LoadVector256(ptr + i);

                        Avx.Store(ptrS + i, Avx.Subtract(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, s, length, a.Length);
            }
        }

        #endregion
    }
}
