using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    public static unsafe class NumericsHelpers
    {
        // TODO: ARM support when .NET 5.0 comes out.

        #region Multiply

        /// <summary>
        ///     Multiply a by b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                MultiplySse(a, b);
                return;
            }

            MultiplyNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] *= b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplySse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Multiply(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region MultiplyByScalar

        /// <summary>
        ///     Multiply a by scalar b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Multiply(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                MultiplySse(a, b);
                return;
            }

            MultiplyNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplyNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] *= b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MultiplySse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Multiply(j, scalar));
                }
            }

            if(remainder != 0)
            {
                MultiplyNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region Divide

        /// <summary>
        ///     Divide a by b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                DivideSse(a, b);
                return;
            }

            DivideNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] /= b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideSse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Divide(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region DivideByScalar

        /// <summary>
        ///     Divide a by scalar b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Divide(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                DivideSse(a, b);
                return;
            }

            DivideNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] *= b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void DivideSse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Divide(j, scalar));
                }
            }

            if(remainder != 0)
            {
                DivideNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region Add

        /// <summary>
        ///     Adds b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                AddSse(a, b);
                return;
            }

            AddNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] += b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddSse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Add(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region AddByScalar

        /// <summary>
        ///     Adds scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Add(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                AddSse(a, b);
                return;
            }

            AddNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] += b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AddSse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Add(j, scalar));
                }
            }

            if(remainder != 0)
            {
                AddNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region HorizontalAdd

        /// <summary>
        ///     Adds all elements of a and returns the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float HorizontalAdd(float[] a)
        {
            // TODO: SSE for this.

            return HorizontalAddNaive(a, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static float HorizontalAddNaive(float[] a, int start, int end)
        {
            var sum = 0f;

            for (var i = start; i < end; i++)
            {
                sum += a[i];
            }

            return sum;
        }

        #endregion

        #region Sub

        /// <summary>
        ///     Subtracts b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                SubSse(a, b);
                return;
            }

            SubNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] -= b[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubSse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Subtract(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region SubByScalar

        /// <summary>
        ///     Subtracts scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sub(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                SubSse(a, b);
                return;
            }

            SubNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] -= b;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SubSse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Subtract(j, scalar));
                }
            }

            if(remainder != 0)
            {
                SubNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region Abs

        /// <summary>
        ///     Adds scalar b to a and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Abs(float[] a)
        {
            if (Sse.IsSupported && Sse2.IsSupported)
            {
                AbsSse(a);
                return;
            }

            AbsNaive(a, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsNaive(float[] a, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] = MathF.Abs(a[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void AbsSse(float[] a)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var mask = Sse2.ShiftRightLogical(Vector128.Create(-1), 1).AsSingle();

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.And(mask, j));
                }
            }

            if(remainder != 0)
            {
                AbsNaive(a, length, a.Length);
            }
        }

        #endregion

        #region Min

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                MinSse(a, b);
                return;
            }

            MinNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] = MathF.Min(a[i], b[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinSse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Min(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region MinByScalar

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Min(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                MinSse(a, b);
                return;
            }

            MinNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] = MathF.Min(a[i], b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MinSse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Min(j, scalar));
                }
            }

            if(remainder != 0)
            {
                MinNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region Max

        /// <summary>
        ///     Gets the minimum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length of arrays must be the same!");

            if (Sse.IsSupported)
            {
                MaxSse(a, b);
                return;
            }

            MaxNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxNaive(float[] a, float[] b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] = MathF.Max(a[i], b[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxSse(float[] a, float[] b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            fixed (float* ptr = a)
            {
                fixed (float* ptrB = b)
                {
                    for (var i = 0; i < length; i += 4)
                    {
                        var j = Sse.LoadVector128(ptr + i);
                        var k = Sse.LoadVector128(ptrB + i);

                        Sse.Store(ptr + i, Sse.Max(j, k));
                    }
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, length, a.Length);
            }
        }

        #endregion

        #region MaxByScalar

        /// <summary>
        ///     Gets the maximum number between a and b and stores the result in a.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Max(float[] a, float b)
        {
            if (Sse.IsSupported)
            {
                MaxSse(a, b);
                return;
            }

            MaxNaive(a, b, 0, a.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxNaive(float[] a, float b, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                a[i] = MathF.Max(a[i], b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void MaxSse(float[] a, float b)
        {
            var remainder = a.Length & 3;
            var length = a.Length - remainder;

            var scalar = Vector128.Create(b);

            fixed (float* ptr = a)
            {
                for (var i = 0; i < length; i += 4)
                {
                    var j = Sse.LoadVector128(ptr + i);

                    Sse.Store(ptr + i, Sse.Max(j, scalar));
                }
            }

            if(remainder != 0)
            {
                MaxNaive(a, b, length, a.Length);
            }
        }

        #endregion
    }
}
