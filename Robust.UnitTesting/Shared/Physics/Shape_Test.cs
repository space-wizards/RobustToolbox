using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(IPhysShape))]
    public class Shape_Test : RobustUnitTest
    {
        [Test]
        public void TestPolyNormals()
        {
            var poly = new PolygonShape
            {
                Vertices = new List<Vector2>
                {
                    new(1, -1),
                    new(1, 1),
                    new(-1, 1),
                    new(-1, -1),
                }
            };

            Assert.That(poly.Normals.Count, Is.EqualTo(4));

            Assert.That(poly.Normals[0], Is.EqualTo(new Vector2(1, 0)), $"Vert is {poly.Vertices[0]}");
            Assert.That(poly.Normals[1], Is.EqualTo(new Vector2(0, 1)), $"Vert is {poly.Vertices[1]}");
            Assert.That(poly.Normals[2], Is.EqualTo(new Vector2(-1, 0)), $"Vert is {poly.Vertices[2]}");
            Assert.That(poly.Normals[3], Is.EqualTo(new Vector2(0, -1)), $"Vert is {poly.Vertices[3]}");
        }
    }
}
