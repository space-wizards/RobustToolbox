using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(Angle))]
    public class Angle_Test
    {
        private const double Epsilon = 1.0e-8;

        private static IEnumerable<(float, float, Direction, double)> Sources => new(float, float, Direction, double)[]
        {
            (1, 0, Direction.East, 0.0),
            (1, 1, Direction.NorthEast, System.Math.PI / 4.0),
            (0, 1, Direction.North, System.Math.PI / 2.0),
            (-1, 1, Direction.NorthWest, 3 * System.Math.PI / 4.0),
            (-1, 0, Direction.West, System.Math.PI),
            (-1, -1, Direction.SouthWest, -3 * System.Math.PI / 4.0),
            (0, -1, Direction.South, -System.Math.PI / 2.0),
            (1, -1, Direction.SouthEast, -System.Math.PI / 4.0),

            (0.92387953251128674f, -0.38268343236508978f, Direction.East, -System.Math.PI / 8.0)
        };

        [Test]
        public void TestAngleZero()
        {
            Assert.That(Angle.Zero.Theta, Is.AtMost(Epsilon));
            Assert.That(Angle.Zero.Degrees, Is.AtMost(Epsilon));
        }

        [Test]
        public void TestAngleEqualsApprox([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var control = new Angle(new Vector2(test.Item1, test.Item2));
            var target = new Angle(test.Item4);
            Angle targetPlusRev = target + MathHelper.TwoPi;
            Angle targetMinusRev = target - MathHelper.TwoPi;

            Assert.That(target.EqualsApprox(control));
            Assert.That(targetPlusRev.EqualsApprox(control));
            Assert.That(targetMinusRev.EqualsApprox(control));
        }

        [Test]
        public void TestAngleEqualsApproxWithTolerance([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var control = new Angle(new Vector2(test.Item1, test.Item2));
            var target = new Angle(test.Item4);
            Angle targetPlusRev = target + MathHelper.TwoPi;
            Angle targetMinusRev = target - MathHelper.TwoPi;

            Assert.That(target.EqualsApprox(control, 0.00001));
            Assert.That(targetPlusRev.EqualsApprox(control, 0.00001));
            Assert.That(targetMinusRev.EqualsApprox(control, 0.00001));

            Angle targetWithLargeDelta = target + 1;
            Angle targetWithSmallDelta = target + 0.01;

            // Large delta shouldn't be accepted, even with a large tolerance.
            Assert.That(targetWithLargeDelta.EqualsApprox(control, 0.1), Is.False);

            // Small detla should be accepted with a large tolerance, but not with small tolerance.
            Assert.That(targetWithSmallDelta.EqualsApprox(control, 0.1));
            Assert.That(targetWithSmallDelta.EqualsApprox(control, 0.00001), Is.False);
        }

        [Test]
        public void TestAngleFromDegrees([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var rads = test.Item4;
            var degrees = MathHelper.RadiansToDegrees(rads);

            var target = Angle.FromDegrees(degrees);
            var control = new Angle(rads);

            Assert.That(target.EqualsApprox(control));
        }

        [Test]
        public void TestAngleToDoubleImplicitConversion([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var control = new Angle(new Vector2(test.Item1, test.Item2));

            double impl = control;
            var expl = (double) control;

            Assert.That(impl, Is.EqualTo(expl).Within(Epsilon));
        }

        [Test]
        public void TestDoubleToAngleImplicitConversion([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var rads = test.Item4;

            Angle impl = rads;
            var expl = new Angle(rads);

            Assert.That(impl.EqualsApprox(expl));
        }

        [Test]
        public void TestFloatToAngleImplicitConversion([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var rads = (float) test.Item4;

            Angle impl = rads;
            var expl = new Angle(rads);

            Assert.That(impl.EqualsApprox(expl));
        }

        [Test]
        [Sequential]
        public void TestAngleToVector2([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var control = new Vector2(test.Item1, test.Item2).Normalized;
            var target = new Angle(test.Item4);

            Assert.That((control - target.ToVec()).LengthSquared, Is.AtMost(Epsilon));
        }

        [Test]
        [TestCase(MathHelper.PiOver2, ExpectedResult = Direction.East)]
        [TestCase(0, ExpectedResult = Direction.South)]
        [TestCase(-MathHelper.PiOver2, ExpectedResult = Direction.West)]
        [TestCase(Math.PI, ExpectedResult = Direction.North)]
        public Direction TestAngleToCardinal(double angle)
        {
            return new Angle(angle).GetCardinalDir();
        }

        [Test]
        public void TestAngleRotateVec()
        {
            var angle = new Angle(MathHelper.Pi / 6);
            var vec = new Vector2(0.5f, 0.5f);

            var result = angle.RotateVec(vec);

            Assert.That(result, new ApproxEqualityConstraint(new Vector2(0.183013f, 0.683013f), 0.001));
        }

        [TestCase(0, 4, ExpectedResult = 0f)]
        [TestCase(30, 4, ExpectedResult = 30f)]
        [TestCase(45, 4, ExpectedResult = -45f)]
        [TestCase(90, 4, ExpectedResult = 0f)]
        [TestCase(120, 4, ExpectedResult = 30f)]
        [TestCase(135, 4, ExpectedResult = -45f)]
        public float UnwindClamp(float theta, int divisions)
        {
            var segSize = 360f / (divisions * 2);
            var segments = (int)(theta / segSize);
            var odd = segments % 2;
            var result = theta - (segments * segSize) - (odd * segSize);

            return result;
        }
    }
}
