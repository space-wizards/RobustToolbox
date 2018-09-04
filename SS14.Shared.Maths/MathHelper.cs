#region --- License ---

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

namespace SS14.Shared.Maths
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
        public const float Pi = 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930382f;

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
        public const float E = 2.71828182845904523536f;

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
        public static long NextPowerOfTwo(long n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (long)NextPowerOfTwo((double) n);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static int NextPowerOfTwo(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (int)NextPowerOfTwo((double) n);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static float NextPowerOfTwo(float n)
        {
            if (float.IsNaN(n) || float.IsInfinity(n)) throw new ArgumentOutOfRangeException(nameof(n), "Must be a number.");
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");
            return (float)NextPowerOfTwo((double) n);
        }

        /// <summary>
        /// Returns the next power of two that is larger than the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static double NextPowerOfTwo(double n)
        {
            if (double.IsNaN(n) || double.IsInfinity(n)) throw new ArgumentOutOfRangeException(nameof(n), "Must be a number.");
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Must be positive.");

            // Don't return negative powers of two, that's nonsense.
            if (n < 1) return 1.0;

            return Math.Pow(2, Math.Floor(Math.Log(n, 2)) + 1);
        }

        #endregion NextPowerOfTwo

        #region Factorial

        /// <summary>Calculates the factorial of a given natural number.
        /// </summary>
        /// <param name="n">The number.</param>
        /// <returns>n!</returns>
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
        public static float DegreesToRadians(float degrees)
        {
            const float degToRad = (float) Math.PI / 180.0f;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        public static float RadiansToDegrees(float radians)
        {
            const float radToDeg = 180.0f / (float) Math.PI;
            return radians * radToDeg;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
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
        public static void Swap(ref float a, ref float b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        #endregion Swap

        #region MinMax

        public static float Min(float a, float b, float c, float d)
        {
            return Math.Min(a, Math.Min(b, Math.Min(c, d)));
        }

        public static float Max(float a, float b, float c, float d)
        {
            return Math.Max(a, Math.Max(b, Math.Max(c, d)));
        }

        /// <summary>
        /// Returns the median value out of a, b and c.
        /// </summary>
        /// <returns>THe median.</returns>
        public static float Median(float a, float b, float c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        #endregion MinMax

        /// <summary>
        ///     This method provides floored modulus.
        ///     C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [DebuggerStepThrough]
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
        public static int Mod(int n, int d)
        {
            var r = n % d;
            return r < 0 ? r + d : r;
        }

        #endregion Public Members
    }
}
