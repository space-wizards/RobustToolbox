using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace Robust.Shared.Maths
{
    public static unsafe class NumericsHelpers
    {
        #region Constructor & Environment Variables

        public const string DisabledEnvironmentVariable = "ROBUST_NUMERICS_DISABLED";
        public const string AvxEnvironmentVariable = "ROBUST_NUMERICS_AVX";

        /// <summary>
        ///     Whether to use the hardware-accelerated paths.
        /// </summary>
        public static readonly bool Enabled;

        /// <summary>
        ///     Whether AVX is enabled.
        /// </summary>
        public static readonly bool AvxEnabled;

        static NumericsHelpers()
        {
            var disabled = Environment.GetEnvironmentVariable(DisabledEnvironmentVariable);
            var avxEnabled = Environment.GetEnvironmentVariable(AvxEnvironmentVariable);
            Enabled = disabled == null || !bool.Parse(disabled);
            AvxEnabled = Enabled && Avx.IsSupported && avxEnabled != null && bool.Parse(avxEnabled);
        }

        #endregion

        #region Utils

        /// <summary>
        ///     Returns whether the specified array length is valid for loading into 128-bit registers.
        /// </summary>
        private static bool LengthValid128Single(int arrayLength)
        {
            return arrayLength >= 4;
        }

        /// <summary>
        ///     Returns whether the specified array length is valid for loading into 256-bit registers.
        /// </summary>
        private static bool LengthValid256Single(int arrayLength)
        {
            return arrayLength >= 8;
        }

        #endregion

        #region Multiply

        /// <summary>
        ///     Multiplies a by b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(Span<float> a, ReadOnlySpan<float> b)
        {
            Multiply(a, b, a);
        }

        /// <summary>
        ///     Multiplies a by b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    MultiplyAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MultiplySse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MultiplyAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MultiplyNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] * b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplySse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Sse.Store(ptrS + i, Sse.Multiply(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            AdvSimd.Store(ptrS + i, AdvSimd.Multiply(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Avx.Store(ptrS + i, Avx.Multiply(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region MultiplyByScalar

        /// <summary>
        ///     Multiply a by scalar b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(Span<float> a, float b)
        {
            Multiply(a, b, a);
        }

        /// <summary>
        ///     Multiply a by scalar b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    MultiplyAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MultiplySse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MultiplyAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MultiplyNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] * b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplySse(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Sse.Store(ptrS + i, Sse.Multiply(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        AdvSimd.Store(ptrS + i, AdvSimd.Multiply(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyAvx(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Avx.Store(ptrS + i, Avx.Multiply(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region Divide

        /// <summary>
        ///     Divide a by b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(Span<float> a, ReadOnlySpan<float> b)
        {
            Divide(a, b, a);
        }

        /// <summary>
        ///     Divide a by b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    DivideAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        DivideSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        DivideAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            DivideNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] / b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideSse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Sse.Store(ptrS + i, Sse.Divide(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            AdvSimd.Store(ptrS + i, AdvSimd.Arm64.Divide(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Avx.Store(ptrS + i, Avx.Divide(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region DivideByScalar

        /// <summary>
        ///     Divide a by scalar b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(Span<float> a, float b)
        {
            Divide(a, b, a);
        }

        /// <summary>
        ///     Divide a by scalar b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    DivideAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        DivideSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        DivideAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            DivideNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = a[i] / b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideSse(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Sse.Store(ptrS + i, Sse.Divide(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        AdvSimd.Store(ptrS + i, AdvSimd.Arm64.Divide(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideAvx(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Avx.Store(ptrS + i, Avx.Divide(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

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

        #region Min

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(Span<float> a, ReadOnlySpan<float> b)
        {
            Min(a, b, a);
        }

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    MinAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MinSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MinAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MinNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Min(a[i], b[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinSse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Sse.Store(ptrS + i, Sse.Min(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            AdvSimd.Store(ptrS + i, AdvSimd.Min(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Avx.Store(ptrS + i, Avx.Min(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region MinByScalar

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(Span<float> a, float b)
        {
            Min(a, b, a);
        }

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    MinAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MinSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MinAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MinNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Min(a[i], b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinSse(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Sse.Store(ptrS + i, Sse.Min(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        AdvSimd.Store(ptrS + i, AdvSimd.Min(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinAvx(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Avx.Store(ptrS + i, Avx.Min(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region Max

        /// <summary>
        ///     Gets the maximum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(Span<float> a, ReadOnlySpan<float> b)
        {
            Max(a, b, a);
        }

        /// <summary>
        ///     Gets the maximum number between a and b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
        {
            if (a.Length != b.Length || a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if(AvxEnabled && LengthValid256Single(a.Length))
                {
                    MaxAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MaxSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MaxAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MaxNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxNaive(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Max(a[i], b[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxSse(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Sse.Store(ptrS + i, Sse.Max(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxAdvSimd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            AdvSimd.Store(ptrS + i, AdvSimd.Max(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxAvx(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> s)
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

                            Avx.Store(ptrS + i, Avx.Max(j, k));
                        }
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        #endregion

        #region MaxByScalar

        /// <summary>
        ///     Gets the maximum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(Span<float> a, float b)
        {
            Max(a, b, a);
        }

        /// <summary>
        ///     Gets the maximum number between a and b and stores the result in s.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(ReadOnlySpan<float> a, float b, Span<float> s)
        {
            if (a.Length != s.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Enabled)
            {
                if (AvxEnabled && LengthValid256Single(a.Length))
                {
                    MaxAvx(a, b, s);
                    return;
                }

                if (LengthValid128Single(a.Length))
                {
                    if (Sse.IsSupported)
                    {
                        MaxSse(a, b, s);
                        return;
                    }

                    if (AdvSimd.IsSupported)
                    {
                        MaxAdvSimd(a, b, s);
                        return;
                    }
                }
            }

            MaxNaive(a, b, s, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxNaive(ReadOnlySpan<float> a, float b, Span<float> s, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                s[i] = MathF.Max(a[i], b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxSse(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Sse.Store(ptrS + i, Sse.Max(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxAdvSimd(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        AdvSimd.Store(ptrS + i, AdvSimd.Max(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxAvx(ReadOnlySpan<float> a, float b, Span<float> s)
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

                        Avx.Store(ptrS + i, Avx.Max(j, scalar));
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, s, length, a.Length);
            }
        }

        #endregion
    }
}
