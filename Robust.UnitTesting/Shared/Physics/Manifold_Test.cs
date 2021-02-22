using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(Manifold))]
    public class Manifold_Test
    {
        [Test]
        public void TestCopy()
        {
            var oldManifold = new Manifold
            {
                LocalNormal = Vector2.One,
                LocalPoint = Vector2.One,
                Points = {[0] = new ManifoldPoint {LocalPoint = Vector2.One}, [1] = new ManifoldPoint()}
            };

            var manifold = oldManifold.Clone();

            Assert.That(manifold.Points[0] != oldManifold.Points[0]);
        }
    }
}
