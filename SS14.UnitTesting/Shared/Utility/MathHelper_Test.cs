using System.Collections.Generic;
using NUnit.Framework;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Shared.Utility
{
    [TestFixture, Parallelizable]
    [TestOf(typeof(MathHelper))]
    public class MathHelper_Test
    {
        public static IEnumerable<(int val, int mod, int result)> IntModData = new(int, int, int)[]
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
    }
}
