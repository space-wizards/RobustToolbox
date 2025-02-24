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
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class PhysicsTestBedTest : RobustIntegrationTest
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
        var physSystem = entitySystemManager.GetEntitySystem<SharedPhysicsSystem>();
        var gravSystem = entitySystemManager.GetEntitySystem<Gravity2DController>();
        var transformSystem = entitySystemManager.GetEntitySystem<SharedTransformSystem>();
        MapId mapId;

        const int columnCount = 1;
        const int rowCount = 15;
        Entity<PhysicsComponent>[] bodies = new Entity<PhysicsComponent>[columnCount * rowCount];
        Vector2 firstPos = Vector2.Zero;

        await server.WaitPost(() =>
        {
            var mapUid = entityManager.System<SharedMapSystem>().CreateMap(out mapId);
            gravSystem.SetGravity(mapUid, new Vector2(0f, -9.8f));

            var groundUid = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = entityManager.AddComponent<PhysicsComponent>(groundUid);
            var groundManager = entityManager.EnsureComponent<FixturesComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            fixtureSystem.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 1, 1, true), manager: groundManager, body: ground);

            var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
            fixtureSystem.CreateFixture(groundUid, "fix2", new Fixture(vertical, 1, 1, true), manager: groundManager, body: ground);

            physSystem.WakeBody(groundUid, manager: groundManager, body: ground);

            var xs = new[]
            {
                0.0f, -10.0f, -5.0f, 5.0f, 10.0f
            };

            for (var j = 0; j < columnCount; j++)
            {
                for (var i = 0; i < rowCount; i++)
                {
                    var x = 0.0f;

                    var boxUid = entityManager.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                    var box = entityManager.AddComponent<PhysicsComponent>(boxUid);
                    var manager = entityManager.EnsureComponent<FixturesComponent>(boxUid);

                    physSystem.SetBodyType(boxUid, BodyType.Dynamic, manager: manager, body: box);
                    var poly = new PolygonShape(0.001f);
                    poly.Set(new List<Vector2>()
                    {
                        new(0.5f, -0.5f),
                        new(0.5f, 0.5f),
                        new(-0.5f, 0.5f),
                        new(-0.5f, -0.5f),
                    });

                    fixtureSystem.CreateFixture(boxUid, "fix1", new Fixture(poly, 1, 1, true), manager: manager, body: box);
                    physSystem.WakeBody(boxUid, manager: manager, body: box);

                    bodies[j * rowCount + i] = (boxUid, box);
                }
            }

            var bodyOne = bodies[0].Owner;
            firstPos = transformSystem.GetWorldPosition(bodyOne);
        });

        await server.WaitRunTicks(1);

        // Check that gravity workin
        await server.WaitAssertion(() =>
        {
            var tempQualifier = bodies[0].Owner;
            Assert.That(firstPos, Is.Not.EqualTo(transformSystem.GetWorldPosition(tempQualifier)));
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
                    var worldPos = transformSystem.GetWorldPosition(body);

                    // TODO: Multi-column support but I cbf right now
                    // Can't be more exact as some level of sinking is allowed.
                    Assert.That(worldPos.EqualsApprox(new Vector2(0.0f, i + 0.5f), 0.2f), $"Expected y-value of {i + 0.5f} but found {worldPos.Y}");
                    Assert.That(!body.Comp.Awake, $"Body {i} wasn't asleep");
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
        var physSystem = entitySystemManager.GetEntitySystem<SharedPhysicsSystem>();
        var gravSystem = entitySystemManager.GetEntitySystem<Gravity2DController>();
        var transformSystem = entitySystemManager.GetEntitySystem<SharedTransformSystem>();
        MapId mapId;

        var columnCount = 1;
        var rowCount = 15;
        Entity<PhysicsComponent>[] bodies = new Entity<PhysicsComponent>[columnCount * rowCount];
        Vector2 firstPos = Vector2.Zero;

        await server.WaitPost(() =>
        {
            var mapUid = entityManager.System<SharedMapSystem>().CreateMap(out mapId);
            gravSystem.SetGravity(mapUid, new Vector2(0f, -9.8f));

            var groundUid = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = entityManager.AddComponent<PhysicsComponent>(groundUid);
            var groundManager = entityManager.EnsureComponent<FixturesComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            fixtureSystem.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 1, 1, true), manager: groundManager, body: ground);

            var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
            fixtureSystem.CreateFixture(groundUid, "fix2", new Fixture(vertical, 1, 1, true), manager: groundManager, body: ground);

            physSystem.WakeBody(groundUid, manager: groundManager, body: ground);

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

                    var circleUid = entityManager.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 1.1f * i), mapId));
                    var circle = entityManager.AddComponent<PhysicsComponent>(circleUid);
                    var manager = entityManager.EnsureComponent<FixturesComponent>(circleUid);

                    physSystem.SetLinearDamping(circleUid, circle, 0.05f);
                    physSystem.SetBodyType(circleUid, BodyType.Dynamic, manager: manager, body: circle);
                    shape = new PhysShapeCircle(0.5f);
                    fixtureSystem.CreateFixture(circleUid, "fix1",  new Fixture(shape, 1, 1, true), manager: manager, body: circle);
                    physSystem.WakeBody(circleUid, manager: manager, body: circle);

                    bodies[j * rowCount + i] = (circleUid, circle);
                }
            }

            EntityUid tempQualifier3 = bodies[0].Owner;
            firstPos = transformSystem.GetWorldPosition(tempQualifier3);
        });

        await server.WaitRunTicks(1);

        // Check that gravity workin
        await server.WaitAssertion(() =>
        {
            EntityUid tempQualifier = bodies[0].Owner;
            Assert.That(firstPos, Is.Not.EqualTo(transformSystem.GetWorldPosition(tempQualifier)));
        });

        // Assert

        await server.WaitRunTicks(215);

        // Assert settled, none below 0, etc.
        await server.WaitAssertion(() =>
        {
            for (var j = 0; j < columnCount; j++)
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    var body = bodies[j * columnCount + i];
                    var worldPos = transformSystem.GetWorldPosition(body);

                    var expectedY = 0.5f + i;

                    // TODO: Multi-column support but I cbf right now
                    // Can't be more exact as some level of sinking is allowed.
                    Assert.That(worldPos.EqualsApproxPercent(new Vector2(0.0f, expectedY), 0.1f), $"Expected y-value of {expectedY} but found {worldPos.Y}");
                    Assert.That(!body.Comp.Awake);
                }
            }
        });
    }
}
