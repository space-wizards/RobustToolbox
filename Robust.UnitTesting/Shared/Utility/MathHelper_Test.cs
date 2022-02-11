using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture, Parallelizable]
    [TestOf(typeof(MathHelper))]
    public sealed class MathHelper_Test
    {
        public static IEnumerable<(long val, long result)> LongNextPowerOfTwoData = new (long, long)[]
        {
            (1L, 2L),
            (2L, 4L),
            (2147483647L, 2147483648L)
        };

        [Test]
        public void TestLongNextPowerOfTwo([ValueSource(nameof(LongNextPowerOfTwoData))] (long, long) data)
        {
            var (val, result) = data;

            Assert.That(MathHelper.NextPowerOfTwo(val), Is.EqualTo(result));
        }

        [Test]
        public void TestLongNextPowerOfTwoNegativeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(-10L); });
        }

        public static IEnumerable<(int val, int result)> IntNextPowerOfTwoData = new (int, int)[]
        {
            (1, 2),
            (2, 4),
            (3, 4)
        };

        [Test]
        public void TestIntNextPowerOfTwo([ValueSource(nameof(IntNextPowerOfTwoData))] (int, int) data)
        {
            var (val, result) = data;

            Assert.That(MathHelper.NextPowerOfTwo(val), Is.EqualTo(result));
        }

        [Test]
        public void TestIntNextPowerOfTwoNegativeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(-10); });
        }

        public static IEnumerable<(float val, float result)> FloatNextPowerOfTwoData = new (float, float)[]
        {
            (0.001f, 1),
            (0.999f, 1),
            (1.001f, 2),
            (2f, 4)
        };

        [Test]
        public void TestFloatNextPowerOfTwo([ValueSource(nameof(FloatNextPowerOfTwoData))] (float, float) data)
        {
            var (val, result) = data;

            Assert.That(MathHelper.NextPowerOfTwo(val), Is.EqualTo(result));
        }

        [Test]
        public void TestFloatNextPowerOfTwoNegativeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(-10f); });
        }

        [Test]
        public void TestFloatNextPowerOfTwoNaNThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(float.NaN); });
        }

        [Test]
        public void TestFloatNextPowerOfTwoInfinityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(float.PositiveInfinity); });
        }

        public static IEnumerable<(double val, double result)> DoubleNextPowerOfTwoData = new (double, double)[]
        {
            (0.0000001, 1),
            (0.9999999, 1),
            (2.0, 4)
        };

        [Test]
        public void TestDoubleNextPowerOfTwo([ValueSource(nameof(DoubleNextPowerOfTwoData))] (double, double) data)
        {
            var (val, result) = data;

            Assert.That(MathHelper.NextPowerOfTwo(val), Is.EqualTo(result));
        }

        [Test]
        public void TestDoubleNextPowerOfTwoNegativeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(-10.0); });
        }

        [Test]
        public void TestDoubleNextPowerOfTwoNaNThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(double.NaN); });
        }

        [Test]
        public void TestDoubleNextPowerOfTwoInfinityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { MathHelper.NextPowerOfTwo(double.PositiveInfinity); });
        }

        public static IEnumerable<(int val, long result)> FactorialData = new (int, long)[]
        {
            (0, 1L),
            (1, 1L),
            (2, 2L),
            (3, 6L),
            (20, 2432902008176640000L)
        };

        [Test]
        public void TestFactorial([ValueSource(nameof(FactorialData))] (int, long) data)
        {
            var (val, result) = data;

            Assert.That(MathHelper.Factorial(val), Is.EqualTo(result));
        }

        public static IEnumerable<(int n, int k, long result)> BinomialCoefficientData = new (int, int, long)[]
        {
            (0, 0, 1L),
            (0, 1, 1L),
            (1, 0, 1L),
            (1, 1, 1L),
            (1, 2, 0L),
            (2, 1, 2L),
            (12, 5, 792L),
            (20, 13, 77520L),
        };

        [Test]
        public void TestBinomialCoefficient([ValueSource(nameof(BinomialCoefficientData))] (int, int, long) data)
        {
            var (n, k, result) = data;

            Assert.That(MathHelper.BinomialCoefficient(n, k), Is.EqualTo(result));
        }

        public static IEnumerable<(double deg, double rad)> DegreesToRadiansData = new (double, double)[] {
            (0, 0),
            (1, 0.017453292519943295),
            (57.295779513082323, 1),
            (45, 0.78539816339744828),
            (60, 1.0471975511965976),
            (90, 1.5707963267948966),
            (180, 3.1415926535897931),
            (270, 4.7123889803846897),
            (360, 6.2831853071795862)
        };

        [Test]
        public void TestDegreesToRadiansFloat([ValueSource(nameof(DegreesToRadiansData))] (double, double) data)
        {
            var (deg, rad) = data;

            Assert.That(MathHelper.DegreesToRadians((float)deg), Is.EqualTo((float)rad).Within(0.0001f));
        }

        [Test]
        public void TestRadiansToDegreesFloat([ValueSource(nameof(DegreesToRadiansData))] (double, double) data)
        {
            var (deg, rad) = data;

            Assert.That(MathHelper.RadiansToDegrees((float)rad), Is.EqualTo((float)deg).Within(0.0001f));
        }

        [Test]
        public void TestDegreesToRadiansDouble([ValueSource(nameof(DegreesToRadiansData))] (double, double) data)
        {
            var (deg, rad) = data;

            Assert.That(MathHelper.DegreesToRadians(deg), Is.EqualTo(rad).Within(0.00000000001));
        }

        [Test]
        public void TestRadiansToDegreesDouble([ValueSource(nameof(DegreesToRadiansData))] (double, double) data)
        {
            var (deg, rad) = data;

            Assert.That(MathHelper.RadiansToDegrees(rad), Is.EqualTo(deg).Within(0.00000000001));
        }

        [Test]
        public void TestSwapFloat()
        {
            const float a_original = MathHelper.Pi;
            const float b_original = MathHelper.PiOver2;

            float a = a_original;
            float b = b_original;

            MathHelper.Swap(ref a, ref b);

            Assert.That(b, Is.EqualTo(a_original));
            Assert.That(a, Is.EqualTo(b_original));
        }

        [Test]
        public void TestSwapDouble()
        {
            const double a_original = MathHelper.Pi;
            const double b_original = MathHelper.PiOver2;

            double a = a_original;
            double b = b_original;

            MathHelper.Swap(ref a, ref b);

            Assert.That(b, Is.EqualTo(a_original));
            Assert.That(a, Is.EqualTo(b_original));
        }

        [Test]
        public void TestMin()
        {
            Assert.That(MathHelper.Min(1f, 1f, 1f, 0f), Is.EqualTo(0f));
            Assert.That(MathHelper.Min(1f, 1f, 0f, 1f), Is.EqualTo(0f));
            Assert.That(MathHelper.Min(1f, 0f, 1f, 1f), Is.EqualTo(0f));
            Assert.That(MathHelper.Min(0f, 1f, 1f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void TestMax()
        {
            Assert.That(MathHelper.Max(0f, 0f, 0f, 1f), Is.EqualTo(1f));
            Assert.That(MathHelper.Max(0f, 0f, 1f, 0f), Is.EqualTo(1f));
            Assert.That(MathHelper.Max(0f, 1f, 0f, 0f), Is.EqualTo(1f));
            Assert.That(MathHelper.Max(1f, 0f, 0f, 0f), Is.EqualTo(1f));
        }

        public static IEnumerable<(int val, int mod, int result)> IntModData = new (int, int, int)[]
        {
            (-5, 5, 0),
            (-4, 5, 1),
            (-3, 5, 2),
            (-2, 5, 3),
            (-1, 5, 4),
            (0, 5, 0),
            (1, 5, 1),
            (2, 5, 2),
            (3, 5, 3),
            (4, 5, 4),
            (5, 5, 0),
        };

        [Test]
        public void TestIntMod([ValueSource(nameof(IntModData))] (int, int, int) data)
        {
            var (val, mod, result) = data;

            Assert.That(MathHelper.Mod(val, mod), Is.EqualTo(result));
        }

        public static IEnumerable<(double val, double mod, double result)> DoubleModData = new (double, double, double)[]
        {
            (-5.1, 5, 4.9),
            (-4.9, 5, 0.1),
            (-4.1, 5, 0.9),
            (-3.9, 5, 1.1),
            (-3.1, 5, 1.9),
            (-2.9, 5, 2.1),
            (-2.1, 5, 2.9),
            (-1.9, 5, 3.1),
            (-1.1, 5, 3.9),
            (-0.9, 5, 4.1),
            (-0.1, 5, 4.9),
            (0.0, 5, 0.0),
            (0.1, 5, 0.1),
            (0.9, 5, 0.9),
            (1.1, 5, 1.1),
            (1.9, 5, 1.9),
            (2.1, 5, 2.1),
            (2.9, 5, 2.9),
            (3.1, 5, 3.1),
            (3.9, 5, 3.9),
            (4.1, 5, 4.1),
            (4.9, 5, 4.9),
            (5.1, 5, 0.1),
        };

        [Test]
        public void TestDoubleMod([ValueSource(nameof(DoubleModData))] (double, double, double) data)
        {
            var (val, mod, result) = data;

            Assert.That(MathHelper.Mod(val, mod), Is.EqualTo(result).Within(0.00000000001));
        }
    }
}
