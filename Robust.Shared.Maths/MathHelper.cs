﻿#region --- License ---

/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 *
 * Contributions by Andy Gill, James Talton and Georg Wächter.
 */

#endregion --- License ---

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vec4 = System.Numerics.Vector4;

namespace Robust.Shared.Maths
{
    /// <summary>
    /// Contains common mathematical functions and constants.
    /// </summary>
    public static class MathHelper
    {
        #region Fields

        /// <summary>
        /// Defines the value of Pi as a <see cref="System.Single"/>.
        /// </summary>
        public const float Pi = MathF.PI;

        /// <summary>
        /// Defines the value of Pi divided by two as a <see cref="System.Single"/>.
        /// </summary>
        public const float PiOver2 = Pi / 2;

        /// <summary>
        /// Defines the value of Pi divided by three as a <see cref="System.Single"/>.
        /// </summary>
        public const float PiOver3 = Pi / 3;

        /// <summary>
        /// Definesthe value of  Pi divided by four as a <see cref="System.Single"/>.
        /// </summary>
        public const float PiOver4 = Pi / 4;

        /// <summary>
        /// Defines the value of Pi divided by six as a <see cref="System.Single"/>.
        /// </summary>
        public const float PiOver6 = Pi / 6;

        /// <summary>
        /// Defines the value of Pi multiplied by two as a <see cref="System.Single"/>.
        /// </summary>
        public const float TwoPi = 2 * Pi;

        /// <summary>
        /// Defines the value of Pi multiplied by 3 and divided by two as a <see cref="System.Single"/>.
        /// </summary>
        public const float ThreePiOver2 = 3 * Pi / 2;

        /// <summary>
        /// Defines the value of E as a <see cref="System.Single"/>.
        /// </summary>
        public const float E = MathF.E;

        /// <summary>
        /// Defines the base-10 logarithm of E.
        /// </summary>
        public const float Log10E = 0.434294482f;

        /// <summary>
        /// Defines the base-2 logarithm of E.
        /// </summary>
        public const float Log2E = 1.442695041f;

        #endregion Fields

        #region Public Members

        #region NextPowerOfTwo

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NextPowerOfTwo(long n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return 1L << (BitOperations.Log2((ulong)n) + 1);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return 1 << (BitOperations.Log2((uint)n) + 1);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextPowerOfTwo(float n)
        {
            if (!float.IsFinite(n))
                throw new ArgumentOutOfRangeException(nameof(n), "Must be a number.");
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (float) NextPowerOfTwo((double) n);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextPowerOfTwo(double n)
        {
            if (!double.IsFinite(n))
                throw new ArgumentOutOfRangeException(nameof(n), "Must be a number.");
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");

            // Don't return negative powers of two, that's nonsense.
            if (n < 1) return 1.0;

            return Math.Pow(2, Math.Floor(Math.Log(n, 2)) + 1);
        }

        #endregion NextPowerOfTwo

        #region NextMultipleOf

        /// <summary>
        ///     Returns the next closest multiple of a number.
        /// </summary>
        /// <param name="value">Closest value</param>
        /// <param name="of">Returns the multiple of this number.</param>
        /// <returns>The next closest multiple of a number.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextMultipleOf(double value, double of)
        {
            return Math.Ceiling(value / of) * of;
        }

        /// <summary>
        ///     Returns the next closest multiple of a number.
        /// </summary>
        /// <param name="value">Closest value</param>
        /// <param name="of">Returns the multiple of this number.</param>
        /// <returns>The next closest multiple of a number.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextMultipleOf(float value, float of)
        {
            return MathF.Ceiling(value / of) * of;
        }

        /// <summary>
        ///     Returns the next closest multiple of a number.
        /// </summary>
        /// <param name="value">Closest value</param>
        /// <param name="of">Returns the multiple of this number.</param>
        /// <returns>The next closest multiple of a number.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long NextMultipleOf(long value, long of)
        {
            return ((value + of - 1) / of) * of;
        }

        /// <summary>
        ///     Returns the next closest multiple of a number.
        /// </summary>
        /// <param name="value">Closest value</param>
        /// <param name="of">Returns the multiple of this number.</param>
        /// <returns>The next closest multiple of a number.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextMultipleOf(int value, int of)
        {
            return ((value + of - 1) / of) * of;
        }

        #endregion

        #region Factorial

        /// <summary>Calculates the factorial of a given natural number.
        /// </summary>
        /// <param name="n">The number.</param>
        /// <returns>n!</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Factorial(int n)
        {
            long result = 1;

            for (; n > 1; n--)
                result *= n;

            return result;
        }

        #endregion Factorial

        #region BinomialCoefficient

        /// <summary>
        /// Calculates the binomial coefficient <paramref name="n"/> above <paramref name="k"/>.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="k">The k.</param>
        /// <returns>n! / (k! * (n - k)!)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long BinomialCoefficient(int n, int k)
        {
            return Factorial(n) / (Factorial(k) * Factorial(n - k));
        }

        #endregion BinomialCoefficient

        #region DegreesToRadians

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DegreesToRadians(float degrees)
        {
            const float degToRad = Pi / 180.0f;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RadiansToDegrees(float radians)
        {
            const float radToDeg = 180.0f / Pi;
            return radians * radToDeg;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreesToRadians(double degrees)
        {
            const double degToRad = Math.PI / 180.0;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToDegrees(double radians)
        {
            const double radToDeg = 180.0 / Math.PI;
            return radians * radToDeg;
        }

        #endregion DegreesToRadians

        #region Swap

        /// <summary>
        /// Swaps two double values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref double a, ref double b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        /// <summary>
        /// Swaps two float values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref float a, ref float b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        #endregion Swap

        #region MinMax

        /// <summary>
        /// Returns the minimum of 4 values
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b, float c, float d)
        {
            return MathF.Min(a, MathF.Min(b, MathF.Min(c, d)));
        }

        /// <summary>
        /// Returns the maximum of 4 values
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b, float c, float d)
        {
            return MathF.Max(a, MathF.Max(b, MathF.Max(c, d)));
        }

        /// <summary>
        /// Returns the median value out of a, b and c.
        /// </summary>
        /// <returns>The median.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Median(float a, float b, float c)
        {
            return MathF.Max(MathF.Min(a, b), MathF.Min(MathF.Max(a, b), c));
        }

        public static TimeSpan Min(TimeSpan a, TimeSpan b)
        {
            return a < b ? a : b;
        }

        public static TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return a > b ? a : b;
        }

        #endregion MinMax

        #region Mod

        /// <summary>
        ///     This method provides floored modulus.
        ///     C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Mod(double n, double d)
        {
            return n - Math.Floor(n / d) * d;
        }

        /// <summary>
        ///     This method provides floored modulus.
        ///     C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mod(float n, float d)
        {
            return n - MathF.Floor(n / d) * d;
        }

        /// <summary>
        ///     This method provides floored modulus.
        ///     C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Mod(int n, int d)
        {
            var r = n % d;
            return r < 0 ? r + d : r;
        }

        #endregion Mod

        #region Clamp

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T val, T min, T max)
            where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Clamp(sbyte val, sbyte min, sbyte max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp(byte val, byte min, byte max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Clamp(short val, short min, short max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Clamp(ushort val, ushort min, ushort max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int val, int min, int max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Clamp(uint val, uint min, uint max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clamp(long val, long min, long max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Clamp(ulong val, ulong min, ulong max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float val, float min, float max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        /// Clamps <paramref name="val"/> between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double val, double min, double max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        /// <summary>
        ///     Clamps <paramref name="val"/> between 0 and 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float val)
        {
            return Clamp(val, 0, 1);
        }

        #endregion Clamp

        #region CloseToPercent

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="percentage"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(float a, float b, double percentage = .00001)
        {
            // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            double epsilon = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * percentage, percentage);
            return Math.Abs(a - b) <= epsilon;
        }

        /// <summary>
        /// Returns whether two vectors are within <paramref name="percentage"/> of each other
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(Vec4 a, Vec4 b, float percentage = .00001f)
        {
            a = Vec4.Abs(a);
            b = Vec4.Abs(b);
            var p = new Vec4(percentage);
            var epsilon = Vec4.Max(Vec4.Max(a, b) * p, p);
            var delta = Vec4.Abs(a - b);
            return delta.X <= epsilon.X && delta.Y <= epsilon.Y && delta.Z <= epsilon.Z && delta.W <= epsilon.W;
        }

        /// <summary>
        /// Returns whether two colours are within <paramref name="percentage"/> of each other
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(Color a, Color b, float percentage = .00001f)
            => CloseToPercent(a.RGBA, b.RGBA, percentage);

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="percentage"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(float a, double b, double percentage = .00001)
        {
            // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            double epsilon = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * percentage, percentage);
            return Math.Abs(a - b) <= epsilon;
        }

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="percentage"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(double a, float b, double percentage = .00001)
        {
            // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            double epsilon = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * percentage, percentage);
            return Math.Abs(a - b) <= epsilon;
        }

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="percentage"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseToPercent(double a, double b, double percentage = .00001)
        {
            // .001% of the smaller value for the epsilon check as per MSDN reference suggestion
            double epsilon = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)) * percentage, percentage);
            return Math.Abs(a - b) <= epsilon;
        }

        #endregion CloseToPercent

        #region CloseTo

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="tolerance"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseTo(float a, float b, float tolerance = .0000001f)
        {
            return MathF.Abs(a - b) <= tolerance;
        }

        /// <summary>
        /// Returns whether two floating point numbers are within <paramref name="tolerance"/> of eachother
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CloseTo(double a, double b, double tolerance = .0000001)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        #endregion

        #region Lerp

        /// <summary>
        /// Linearly interpolates between <paramref name="a"/> to <paramref name="b"/>, returning the value at <paramref name="blend"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, double blend)
        {
            return a + (b - a) * blend;
        }

        /// <summary>
        /// Linearly interpolates between <paramref name="a"/> to <paramref name="b"/>, returning the value at <paramref name="blend"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float blend)
        {
            return a + (b - a) * blend;
        }

        public static TimeSpan Lerp(TimeSpan a, TimeSpan b, double t)
        {
            return a + t * (b - a);
        }

        #endregion Lerp

        #region InterpolateCubic

        /// <summary>
        /// Cubic interpolates form <paramref name="a"/> to <paramref name="b"/>, where <paramref name="preA"/> and <paramref name="postB"/> are handles and returns the position at <paramref name="t"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolateCubic(float preA, float a, float b, float postB, float t)
        {
            return a + 0.5f * t * (b - preA + t * (2.0f * preA - 5.0f * a + 4.0f * b - postB + t * (3.0f * (a - b) + postB - preA)));
        }

        /// <summary>
        /// Cubic interpolates form <paramref name="a"/> to <paramref name="b"/>, where <paramref name="preA"/> and <paramref name="postB"/> are handles and returns the position at <paramref name="t"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double InterpolateCubic(double preA, double a, double b, double postB, double t)
        {
            return a + 0.5 * t * (b - preA + t * (2.0 * preA - 5.0 * a + 4.0 * b - postB + t * (3.0 * (a - b) + postB - preA)));
        }

        #endregion InterpolateCubic

        #region Intersections

        // MIT License

        // Copyright (c) 2019 Erin Catto

        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:

        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.

        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        /// <summary>
        /// Gets the intersection between a line and a circle.
        /// Essentially a reduced raycast.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetIntersecting(Vector2 start, Vector2 end, float radius, [NotNullWhen(true)] out Vector2? point)
        {
            var maxFraction = (end - start).Length();
            float b = Vector2.Dot(start, start) - radius * radius;

            // Solve quadratic equation.
            var r = end - start;
            float c =  Vector2.Dot(start, r);
            float rr = Vector2.Dot(r, r);
            float sigma = c * c - rr * b;

            // Check for negative discriminant and short segment.
            if (sigma < 0.0f || rr < float.Epsilon)
            {
                point = null;
                return false;
            }

            // Find the point of intersection of the line with the circle.
            float a = -(c + MathF.Sqrt(sigma));

            // Is the intersection point on the segment?
            if (0.0f <= a && a <= maxFraction * rr)
            {
                a /= rr;
                var lineToEnd = end - start;
                // a is a fraction so need to work out the distance along the line we need to be.
                point = start + lineToEnd * a;
                return true;
            }

            point = null;
            return false;
        }

        #endregion

        /// <summary>
        /// Round up (ceiling) a value to a multiple of a known power of two.
        /// </summary>
        /// <param name="value">The value to round up.</param>
        /// <param name="powerOfTwo">
        /// The power of two to round up to a multiple of. The result is undefined if this is not a power of two.
        /// </param>
        /// <remarks>
        /// The result is undefined if either value is negative.
        /// </remarks>
        /// <typeparam name="T">The type of integer to operate on.</typeparam>
        /// <example>
        /// <code>
        /// MathHelper.CeilMultiplyPowerOfTwo(5, 4) // 8
        /// MathHelper.CeilMultiplyPowerOfTwo(4, 4) // 4
        /// MathHelper.CeilMultiplyPowerOfTwo(8, 4) // 8
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CeilMultipleOfPowerOfTwo<T>(T value, T powerOfTwo) where T : IBinaryInteger<T>
        {
            var mask = powerOfTwo - T.One;
            var remainder = value & mask;
            return remainder == T.Zero ? value : (value | mask) + T.One;
        }

        #endregion Public Members
    }
}
