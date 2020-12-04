using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [TestOf(typeof(Box2Rotated))]
    public class Box2Rotated_Test
    {
        private static IEnumerable<Box2[]> BoxRotations = new[]
        {
            new[]{
                new Box2(0.5f, -0.2f, 0.5f, 0.5f),
                new Box2(-0.5f, 0.5f, 0.2f, 0.5f),
                new Box2(-0.5f, -0.5f, -0.5f, 0.2f),
                new Box2(-0.2f, -0.5f, 0.5f, -0.5f)
            },
            new[]
            {
                new Box2(0.5f,0.5f,0.5f,0.5f),
                new Box2(-0.5f,0.5f,-0.5f,0.5f),
                new Box2(-0.5f,-0.5f,-0.5f,-0.5f),
                new Box2(0.5f,-0.5f,0.5f,-0.5f)
            }
        };

        [Test]
        public void FullRotationTest([ValueSource(nameof(BoxRotations))] Box2[] boxes)
        {
            var rotatedBox = new Box2Rotated(boxes[0], Angle.FromDegrees(0));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(rotatedBox.CalcBoundingBox(), NUnit.Framework.Is.EqualTo(boxes[i]));
                rotatedBox.Rotation += Angle.FromDegrees(90);
            }
            Assert.That(rotatedBox.CalcBoundingBox(), NUnit.Framework.Is.EqualTo(boxes[0]));
        }
    }
}
