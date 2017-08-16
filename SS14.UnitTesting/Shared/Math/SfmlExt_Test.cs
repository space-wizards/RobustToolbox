using NUnit.Framework;
using SFML.System;
using SS14.Shared;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace SS14.UnitTesting.Shared.Math
{
    [TestFixture]
    public class SfmlExt_Test : SS14UnitTest
    {
        [Test, Sequential]
        public void TestVector2ToDirection([ValueSource(nameof(sources))] (float, float, Direction) test)
        {
            // x1 and y1 are always 0.
            Vector2f origin = new Vector2f(0, 0);
            Vector2f target = new Vector2f(test.Item1, test.Item2);

            Assert.That(origin.DirectionTo(target), Is.EqualTo(test.Item3));
        }

        public static IEnumerable<(float, float, Direction)> sources => new (float, float, Direction)[]
        {
            (0, 1, Direction.North),
            (1, 1, Direction.NorthEast),
            (1, 0, Direction.East),
            (1, -1, Direction.SouthEast),
            (0, -1, Direction.South),
            (-1, -1, Direction.SouthWest),
            (-1, 0, Direction.West),
            (-1, 1, Direction.NorthWest),
        };
    }
}
