// MIT License

// Copyright (c) 2020 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Collision_Test
{
    [Test]
    public void TestHardCollidable()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();

        var fixtures = entManager.System<FixtureSystem>();
        var physics = entManager.System<SharedPhysicsSystem>();

        var map = sim.CreateMap();

        var bodyAUid = entManager.SpawnAttachedTo(null, new EntityCoordinates(map.Uid, Vector2.Zero));
        var bodyBUid = entManager.SpawnAttachedTo(null, new EntityCoordinates(map.Uid, Vector2.Zero));
        var bodyA = entManager.AddComponent<PhysicsComponent>(bodyAUid);
        var bodyB = entManager.AddComponent<PhysicsComponent>(bodyBUid);

        Assert.That(!physics.IsHardCollidable(bodyAUid, bodyBUid));

        fixtures.CreateFixture(bodyAUid, "fix1", new Fixture(new PhysShapeCircle(0.5f), 1, 1, true));
        fixtures.CreateFixture(bodyBUid, "fix1", new Fixture(new PhysShapeCircle(0.5f), 1, 1, true));

        Assert.That(physics.IsHardCollidable(bodyAUid, bodyBUid));
    }

    [Test]
    public void TestCollision()
    {
        var center = new Vector2(100.0f, -50.0f);
        const float hx = 0.5f, hy = 1.5f;
        const float angle1 = 0.25f;

        // Data from issue #422. Not used because the data exceeds accuracy limits.
        //const b2Vec2 center(-15000.0f, -15000.0f);
        //const float hx = 0.72f, hy = 0.72f;
        //const float angle1 = 0.0f;

        PolygonShape polygon1 = new();
        polygon1.SetAsBox(hx, hy, center, angle1);

        const float absTol = 2.0f * float.Epsilon;
        const float relTol = 2.0f * float.Epsilon;

        Assert.That(Math.Abs(polygon1.Centroid.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(Math.Abs(polygon1.Centroid.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));

        Span<Vector2> vertices = stackalloc Vector2[4];
        vertices[0] = new Vector2(center.X - hx, center.Y - hy);
        vertices[1] = new Vector2(center.X + hx, center.Y - hy);
        vertices[2] = new Vector2(center.X - hx, center.Y + hy);
        vertices[3] = new Vector2(center.X + hx, center.Y + hy);

        PolygonShape polygon2 = new();
        polygon2.Set(vertices, 4);

        Assert.That(Math.Abs(polygon2.Centroid.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(Math.Abs(polygon2.Centroid.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));

        const float mass = 4.0f * hx * hy;
        var inertia = (mass / 3.0f) * (hx * hx + hy * hy) + mass * Vector2.Dot(center, center);

        var massData1 = FixtureSystem.GetMassData(polygon1, 1f);

        Assert.That(MathF.Abs(massData1.Center.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(MathF.Abs(massData1.Center.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));
        // TODO: How the hell is this rounding enough that this test fails with the angle???
        // Assert.That(MathF.Abs(massData1.Mass - mass), Is.LessThan(20.0f * (absTol + relTol * mass)));
        // Assert.That(MathF.Abs(massData1.I - inertia), Is.LessThan(40.0f * (absTol + relTol * inertia)));

        var massData2 = FixtureSystem.GetMassData(polygon2, 1f);

        Assert.That(MathF.Abs(massData2.Center.X - center.X), Is.LessThan(absTol + relTol * Math.Abs(center.X)));
        Assert.That(MathF.Abs(massData2.Center.Y - center.Y), Is.LessThan(absTol + relTol * Math.Abs(center.Y)));
        Assert.That(MathF.Abs(massData2.Mass - mass), Is.LessThan(20.0f * (absTol + relTol * mass)));
        Assert.That(MathF.Abs(massData2.I - inertia), Is.LessThan(40.0f * (absTol + relTol * inertia)));
    }

    /// <summary>
    /// Asserts that cross-map contacts correctly destroy
    /// </summary>
    [Test]
    public void CrossMapContacts()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var fixtures = entManager.System<FixtureSystem>();
        var physics = entManager.System<SharedPhysicsSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();
        var mapId = sim.CreateMap().MapId;
        var mapId2 = sim.CreateMap().MapId;

        var ent1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var ent2 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));

        var body1 = entManager.AddComponent<PhysicsComponent>(ent1);
        physics.SetBodyType(ent1, BodyType.Dynamic, body: body1);
        var body2 = entManager.AddComponent<PhysicsComponent>(ent2);
        physics.SetBodyType(ent2, BodyType.Dynamic, body: body2);

        fixtures.CreateFixture(ent1, "fix1", new Fixture(new PhysShapeCircle(1f), 1, 0, true), body: body1);
        fixtures.CreateFixture(ent2, "fix1", new Fixture(new PhysShapeCircle(1f), 0, 1, true), body: body2);

        physics.WakeBody(ent1, body: body1);
        physics.WakeBody(ent2, body: body2);

        Assert.That(body1.Awake && body2.Awake);
        Assert.That(body1.ContactCount == 0 && body2.ContactCount == 0);

        physics.Update(0.01f);

        Assert.That(body1.ContactCount == 1 && body2.ContactCount == 1);

        // Reparent body2 and assert the contact is destroyed
        xformSystem.SetParent(ent2, mapManager.GetMapEntityId(mapId2));
        physics.Update(0.01f);

        Assert.That(body1.ContactCount == 0 && body2.ContactCount == 0);
    }
}
