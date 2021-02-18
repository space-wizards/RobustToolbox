using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Add

        /// <summary>
        ///     Adds b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(Span<float> a, ReadOnlySpan<float> b)
        {
            Add(a, b, a);
        }

        /// <summary>
        ///     Adds b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    AddAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        AddSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        AddAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            AddNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] + b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddSse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Sse.Store(ptrS + i, Sse.Add(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            AdvSimd.Store(ptrS + i, AdvSimd.Add(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Avx.Store(ptrS + i, Avx.Add(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region AddByScalar

        /// <summary>
        ///     Adds scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(Span<float> a, float b)
        {
            Add(a, b, a);
        }

        /// <summary>
        ///     Adds scalar b to a and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    AddAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        AddSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        AddAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            AddNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] + b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddSse(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Sse.Store(ptrS + i, Sse.Add(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        AdvSimd.Store(ptrS + i, AdvSimd.Add(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddAvx(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Avx.Store(ptrS + i, Avx.Add(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region HorizontalAdd

        /// <summary>
        ///     Adds all elements of a and returns the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float HorizontalAdd(ReadOnlySpan<float> a)
        {
            if (Enabled)
            {
                if (AvxEnabled && Sse3.IsSupported && LengthValid256Single(a.Length))
                {
                    return HorizontalAddAvx(a);
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse3.IsSupported)
                    {
                        return HorizontalAddSse(a);
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        return HorizontalAddAdvSimd(a);
                    }
                }
            }

            return HorizontalAddNaive(a, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static float HorizontalAddNaive(ReadOnlySpan<float> a, int start, int end)
        {
            var sum = 0f;

            for (var i = start; i < end; i++)
            {
                sum += a[i];
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static float HorizontalAddSse(ReadOnlySpan<float> a)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var accumulator = Vector128.Create(0f);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);
                    accumulator = Sse3.HorizontalAdd(accumulator, j);
                }
            }

            var sum = 0f;
            accumulator = Sse3.HorizontalAdd(Sse3.HorizontalAdd(accumulator, accumulator), accumulator);
            Sse.StoreScalar(&sum, accumulator);

            if(remainder != 0)
            {
                sum += HorizontalAddNaive(a, length, a.Length);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static float HorizontalAddAdvSimd(ReadOnlySpan<float> a)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var accumulator = Vector128.Create(0f);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = AdvSimd.LoadVector128(ptr + i);
                    accumulator = AdvSimd.Arm64.AddPairwise(accumulator, j);
                }
            }

            var sum = 0f;
            accumulator = AdvSimd.Arm64.AddPairwise(AdvSimd.Arm64.AddPairwise(accumulator, accumulator), accumulator);
            AdvSimd.StoreSelectedScalar(&sum, accumulator, 0);

            if(remainder != 0)
            {
                sum += HorizontalAddNaive(a, length, a.Length);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static float HorizontalAddAvx(ReadOnlySpan<float> a)
        {
            var remainder = a.Length & 7;
            var length = a.Length - remainder;

            var accumulator = Vector128.Create(0f);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 8)
                {
                    var j = Avx.LoadVector256(ptr + i);
                    var x128 = Sse.Add(Avx.ExtractVector128(j, 0), Avx.ExtractVector128(j, 1));
                    accumulator = Sse3.HorizontalAdd(x128, accumulator);
                }
            }

            var sum = 0f;
            accumulator = Sse3.HorizontalAdd(Sse3.HorizontalAdd(accumulator, accumulator), accumulator);
            Sse.StoreScalar(&sum, accumulator);

            if(remainder != 0)
            {
                sum += HorizontalAddNaive(a, length, a.Length);
            }

            return sum;
        }

        #endregion
    }
}
