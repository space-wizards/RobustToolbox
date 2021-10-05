using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    internal class Direction_Test
    {
        private const double Epsilon = 1.0e-8;

        private static IEnumerable<(float x, float y, Direction dir, double angle)> sources => new(float, float, Direction, double)[]
        {
            (1, 0, Direction.East, Angle.FromDegrees(90)),
            (1, 1, Direction.NorthEast, Angle.FromDegrees(135)),
            (0, 1, Direction.North, Angle.FromDegrees(180)),
            (-1, 1, Direction.NorthWest, Angle.FromDegrees(-135)),
            (-1, 0, Direction.West, Angle.FromDegrees(-90)),
            (-1, -1, Direction.SouthWest, Angle.FromDegrees(-45)),
            (0, -1, Direction.South, 0),
            (1, -1, Direction.SouthEast, Angle.FromDegrees(45))
        };

        [Test]
        [Sequential]
        public void TestDirectionToAngle([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var control = test.Item4;
            var val = test.Item3.ToAngle();

            Assert.That(val.Theta, Is.EqualTo(control).Within(Epsilon));
        }

        [Test]
        [Sequential]
        public void TestDirectionToVec([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var control = new Vector2(test.Item1, test.Item2).Normalized;
            var controlInt = new Vector2i((int)test.Item1, (int)test.Item2);
            var val = test.Item3.ToVec();
            var intVec = test.Item3.ToIntVec();

            Assert.That(val, Is.Approximately(control));
            Assert.That(intVec, Is.Approximately(controlInt));
        }

        [Test]
        [Sequential]
        public void TestVecToAngle([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var target = new Vector2(test.Item1, test.Item2).Normalized;

            Assert.That(target.ToWorldAngle().Reduced(), Is.Approximately(new Angle(test.Item4)));
        }

        [Test]
        [Sequential]
        public void TestVector2ToDirection([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var target = new Vector2(test.Item1, test.Item2).Normalized;

            Assert.That(target.GetDir(), Is.EqualTo(test.Item3));
        }

        [Test]
        [Sequential]
        public void TestAngleToDirection([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var target = new Angle(test.Item4);

            Assert.That(target.GetDir(), Is.EqualTo(test.Item3));
        }

    }
}
