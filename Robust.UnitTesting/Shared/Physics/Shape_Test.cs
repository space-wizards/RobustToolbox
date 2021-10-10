using System;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(IPhysShape))]
    public class Shape_Test : RobustUnitTest
    {
        [Test]
        public void TestPolyNormals()
        {
            var poly = new PolygonShape();

            Span<Vector2> verts = stackalloc Vector2[4];

            verts[0] = new Vector2(1f, -1f);
            verts[1] = new Vector2(1f, 1f);
            verts[2] = new Vector2(-1f, 1f);
            verts[3] = new Vector2(-1f, -1f);

            poly.SetVertices(verts);

            Assert.That(poly.Normals.Length, Is.EqualTo(4));

            Assert.That(poly.Normals[0], Is.EqualTo(new Vector2(1, 0)), $"Vert is {poly.Vertices[0]}");
            Assert.That(poly.Normals[1], Is.EqualTo(new Vector2(0, 1)), $"Vert is {poly.Vertices[1]}");
            Assert.That(poly.Normals[2], Is.EqualTo(new Vector2(-1, 0)), $"Vert is {poly.Vertices[2]}");
            Assert.That(poly.Normals[3], Is.EqualTo(new Vector2(0, -1)), $"Vert is {poly.Vertices[3]}");
        }
    }
}
