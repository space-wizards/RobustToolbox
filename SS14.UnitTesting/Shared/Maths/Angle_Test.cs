using System.Collections.Generic;
using NUnit.Framework;
using OpenTK;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.UnitTesting.Shared.Maths
{
    [TestFixture]
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

        private static IEnumerable<(float, float, Direction)> CardinalSources => new(float, float, Direction)[]
        {
            (1, 0, Direction.East),
            (0, 1, Direction.North),
            (-1, 0, Direction.West),
            (0, -1, Direction.South),
        };

        [Test]
        [Sequential]
        public void TestAngleToVector2([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var control = new Vector2(test.Item1, test.Item2).Normalized;
            var target = new Angle(test.Item4);

            Assert.That((control - target.ToVec()).LengthSquared, Is.AtMost(Epsilon));
        }

        [Test]
        [Sequential]
        public void TestAngleToDirection([ValueSource(nameof(Sources))] (float, float, Direction, double) test)
        {
            var target = new Angle(test.Item4);

            Assert.That(target.GetDir(), Is.EqualTo(test.Item3));
        }

        [Test]
        [Sequential]
        public void TestAngleToCardinal([ValueSource(nameof(CardinalSources))] (float, float, Direction) test)
        {
            var target = new Vector2(test.Item1, test.Item2).ToAngle();

            Assert.That(target.GetCardinalDir(), Is.EqualTo(test.Item3));
        }
    }
}
