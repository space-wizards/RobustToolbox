/*
MIT License

Copyright (c) 2019 Erin Catto

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

These tests are derived from box2d's testbed tests but done in a way as to be automated and useful for CI.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    public class PhysicsTestBedTest : RobustIntegrationTest
    {
        [Test]
        public async Task TestBoxStack()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var fixtureSystem = entitySystemManager.GetEntitySystem<FixtureSystem>();
            MapId mapId;

            const int columnCount = 1;
            const int rowCount = 15;
            PhysicsComponent[] bodies = new PhysicsComponent[columnCount * rowCount];
            Vector2 firstPos = Vector2.Zero;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();

                IEntity tempQualifier2 = mapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier2.Uid).Gravity = new Vector2(0, -9.8f);

                IEntity tempQualifier = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
                var ground = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier);

                var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
                var horizontalFixture = new Fixture(ground, horizontal)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };

                fixtureSystem.CreateFixture(ground, horizontalFixture);

                var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
                var verticalFixture = new Fixture(ground, vertical)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };

                fixtureSystem.CreateFixture(ground, verticalFixture);

                var xs = new[]
                {
                    0.0f, -10.0f, -5.0f, 5.0f, 10.0f
                };

                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < rowCount; i++)
                    {
                        var x = 0.0f;

                        IEntity tempQualifier1 = entityManager.SpawnEntity(null,
                            new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                        var box = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier1);

                        box.BodyType = BodyType.Dynamic;
                        var poly = new PolygonShape(0.001f);
                        poly.SetVertices(new List<Vector2>()
                        {
                            new(0.5f, -0.5f),
                            new(0.5f, 0.5f),
                            new(-0.5f, 0.5f),
                            new(-0.5f, -0.5f),
                        });

                        var fixture = new Fixture(box, poly)
                        {
                            CollisionMask = 1,
                            CollisionLayer = 1,
                            Hard = true,
                        };

                        fixtureSystem.CreateFixture(box, fixture);

                        bodies[j * rowCount + i] = box;
                    }
                }

                firstPos = bodies[0].Owner.Transform.WorldPosition;
            });

            await server.WaitRunTicks(1);

            // Check that gravity workin
            await server.WaitAssertion(() =>
            {
                Assert.That(firstPos != bodies[0].Owner.Transform.WorldPosition);
            });

            // Assert

            await server.WaitRunTicks(150);

            // Assert settled, none below 0, etc.
            await server.WaitAssertion(() =>
            {
                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < bodies.Length; i++)
                    {
                        var body = bodies[j * columnCount + i];
                        var worldPos = body.Owner.Transform.WorldPosition;

                        // TODO: Multi-column support but I cbf right now
                        // Can't be more exact as some level of sinking is allowed.
                        Assert.That(worldPos.EqualsApprox(new Vector2(0.0f, i + 0.5f), 0.1f), $"Expected y-value of {i + 0.5f} but found {worldPos.Y}");
                    }
                }
            });
        }

        [Test]
        public async Task TestCircleStack()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var fixtureSystem = entitySystemManager.GetEntitySystem<FixtureSystem>();
            MapId mapId;

            var columnCount = 1;
            var rowCount = 15;
            PhysicsComponent[] bodies = new PhysicsComponent[columnCount * rowCount];
            Vector2 firstPos = Vector2.Zero;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                IEntity tempQualifier2 = mapManager.GetMapEntity(mapId);
                IoCManager.Resolve<IEntityManager>().GetComponent<SharedPhysicsMapComponent>(tempQualifier2.Uid).Gravity = new Vector2(0, -9.8f);

                IEntity tempQualifier = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
                var ground = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier);

                var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
                var horizontalFixture = new Fixture(ground, horizontal)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };

                fixtureSystem.CreateFixture(ground, horizontalFixture);

                var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
                var verticalFixture = new Fixture(ground, vertical)
                {
                    CollisionLayer = 1,
                    CollisionMask = 1,
                    Hard = true
                };

                fixtureSystem.CreateFixture(ground, verticalFixture);

                var xs = new[]
                {
                    0.0f, -10.0f, -5.0f, 5.0f, 10.0f
                };

                PhysShapeCircle shape;

                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < rowCount; i++)
                    {
                        var x = 0.0f;

                        IEntity tempQualifier1 = entityManager.SpawnEntity(null,
                            new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                        var circle = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier1);

                        circle.LinearDamping = 0.05f;
                        circle.BodyType = BodyType.Dynamic;
                        shape = new PhysShapeCircle {Radius = 0.5f};

                        var fixture = new Fixture(circle, shape)
                        {
                            CollisionMask = 1,
                            CollisionLayer = 1,
                            Hard = true,
                        };

                        fixtureSystem.CreateFixture(circle, fixture);

                        bodies[j * rowCount + i] = circle;
                    }
                }

                firstPos = bodies[0].Owner.Transform.WorldPosition;
            });

            await server.WaitRunTicks(1);

            // Check that gravity workin
            await server.WaitAssertion(() =>
            {
                Assert.That(firstPos != bodies[0].Owner.Transform.WorldPosition);
            });

            // Assert

            await server.WaitRunTicks(200);

            // Assert settled, none below 0, etc.
            await server.WaitAssertion(() =>
            {
                for (var j = 0; j < columnCount; j++)
                {
                    for (var i = 0; i < bodies.Length; i++)
                    {
                        var body = bodies[j * columnCount + i];
                        var worldPos = body.Owner.Transform.WorldPosition;

                        var expectedY = 0.5f + i;

                        // TODO: Multi-column support but I cbf right now
                        // Can't be more exact as some level of sinking is allowed.
                        Assert.That(worldPos.EqualsApproxPercent(new Vector2(0.0f, expectedY), 0.1f), $"Expected y-value of {expectedY} but found {worldPos.Y}");
                    }
                }
            });
        }
    }
}
