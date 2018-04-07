using NUnit.Framework;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Client.Utility
{
    [TestFixture]
    class GodotConversionsTest
    {
        [Test]
        public void TestTransform2DConversion()
        {
            var matrix = Matrix3.Identity;
            var xform = matrix.Convert();
            var vec = new Vector2(1, 1);

            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));

            matrix = new Matrix3(2, 0.00f, 5,
                                 0, 0.33f, 5,
                                 0, 0.00f, 1);
            xform = matrix.Convert();
            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));
            vec = Vector2.Zero;
            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));
        }

        private static Vector2 Xform(Vector2 vector, Matrix3 matrix)
        {
            var vec3 = new Vector3(vector.X, vector.Y, 1);
            matrix.Transform(ref vec3);
            return new Vector2(vec3.X, vec3.Y);
        }

        private static Vector2 Xform(Vector2 vector, Godot.Transform2D transform)
        {
            return transform.Xform(vector.Convert()).Convert();
        }
    }
}
