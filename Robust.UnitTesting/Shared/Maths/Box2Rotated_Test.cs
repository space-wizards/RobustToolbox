using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [TestOf(typeof(Box2Rotated))]
    public class Box2Rotated_Test
    {
        public Box2[] Boxes1 = {
            new Box2(0.5f, -0.2f, 0.5f, 0.5f),
            new Box2(-0.2f, 0.5f, 0.5f, 0.5f),
            new Box2(0.5f, 0.5f, 0.5f, -0.2f),
            new Box2(0.5f, 0.5f, -0.2f, 0.5f)
        };

        [Test]
        public void FullRotationTest()
        {
            var angle = Angle.FromDegrees(0);
            for (int i = 0; i < Boxes1.Length; i++)
            {
                var next = i + 1;
                if (next == Boxes1.Length)
                    next = 0;
                var box = Boxes1[i];
                var nextbox = Boxes1[next];

                var rotatedBox = new Box2Rotated(box, angle);
                Assert.Equals(rotatedBox.CalcBoundingBox(), nextbox);
                angle += Angle.FromDegrees(90);
            }
        }
    }
}
