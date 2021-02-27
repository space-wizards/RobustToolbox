using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(ICollisionManager))]
    internal class CollisionManager_Test : RobustIntegrationTest
    {
        [Test(Description = "Tests that every single shape combination returns the correct overlap")]
        public async Task TestShapeOverlaps()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var collisionManager = server.ResolveDependency<ICollisionManager>();
            Transform transformA;
            Transform transformB;

            var circle = new PhysShapeCircle {Radius = 0.5f};
            var poly = new PolygonShape
            {
                Vertices = new List<Vector2>
                {
                    new(-0.5f, -0.5f),
                    new(0.5f, -0.5f),
                    new(0.5f, 0.5f),
                    new(-0.5f, 0.5f),
                }
            };

            await server.WaitAssertion(() =>
            {
                // TODO
                transformA = new Transform();
                transformB = new Transform();
                // TODO: Transforms
                // Test overlaps
                Assert.That(collisionManager.TestOverlap(circle, 0, poly, 0, transformA, transformB),
                    $"No overlap found for circle and polygon!");
            });
        }
    }
}
