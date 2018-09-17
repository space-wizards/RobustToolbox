using NUnit.Framework;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Client.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(GodotConversions))]
    class GodotConversionsTest
    {
        [Test]
        public void TestTransform2DConversion()
        {
            var matrix = Matrix3.Identity;
            var xform = matrix.Convert();
            var vec = new Vector2(5, 7);

            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));

            matrix = new Matrix3(0.75f, 0.20f, 5,
                                 0.66f, 0.15f, 5,
                                 0, 0, 1);
            xform = matrix.Convert();
            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));
            Assert.That(Xform(vec, xform.Convert()), Is.EqualTo(Xform(vec, matrix)));
            vec = Vector2.Zero;
            Assert.That(Xform(vec, xform), Is.EqualTo(Xform(vec, matrix)));
            Assert.That(Xform(vec, xform.Convert()), Is.EqualTo(Xform(vec, matrix)));
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
