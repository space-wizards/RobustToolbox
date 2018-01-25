using System.Collections.Generic;
using NUnit.Framework;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Shared.Maths
{
    [TestFixture]
    internal class Direction_Test
    {
        private const double Epsilon = 1.0e-8;

        private static IEnumerable<(float, float, Direction, double)> sources => new(float, float, Direction, double)[]
        {
            (1, 0, Direction.East, 0.0),
            (1, 1, Direction.NorthEast, System.Math.PI / 4.0),
            (0, 1, Direction.North, System.Math.PI / 2.0),
            (-1, 1, Direction.NorthWest, 3 * System.Math.PI / 4.0),
            (-1, 0, Direction.West, System.Math.PI),
            (-1, -1, Direction.SouthWest, -3 * System.Math.PI / 4.0),
            (0, -1, Direction.South, -System.Math.PI / 2.0),
            (1, -1, Direction.SouthEast, -System.Math.PI / 4.0)
        };

        [Test]
        [Sequential]
        public void TestDirectionToAngle([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var control = test.Item4;
            var val = test.Item3.ToAngle();

            Assert.That(System.Math.Abs(control - val), Is.AtMost(Epsilon));
        }

        [Test]
        [Sequential]
        public void TestDirectionToVec([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var control = new Vector2(test.Item1, test.Item2).Normalized;
            var val = test.Item3.ToVec();

            Assert.That((control - val).LengthSquared, Is.AtMost(Epsilon));
        }

        [Test]
        [Sequential]
        public void TestVecToAngle([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var target = new Vector2(test.Item1, test.Item2).Normalized;

            Assert.That(System.Math.Abs(target.ToAngle() - test.Item4), Is.AtMost(Epsilon));
        }

        [Test]
        [Sequential]
        public void TestVector2ToDirection([ValueSource(nameof(sources))] (float, float, Direction, double) test)
        {
            var target = new Vector2(test.Item1, test.Item2).Normalized;

            Assert.That(target.GetDir(), Is.EqualTo(test.Item3));
        }
    }
}
