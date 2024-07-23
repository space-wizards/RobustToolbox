using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    public void SetRadius(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        IPhysShape shape,
        float radius,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if (MathHelper.CloseTo(shape.Radius, radius) || !Resolve(uid, ref manager, ref body, ref xform))
            return;

        shape.Radius = radius;

        if (body.CanCollide &&
            TryComp<BroadphaseComponent>(xform.Broadphase?.Uid, out var broadphase) &&
            TryComp<PhysicsMapComponent>(xform.MapUid, out var physicsMap))
        {
            _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
        }

        _fixtures.FixtureUpdate(uid, manager: manager, body: body);
    }

    #region Circle

    public void SetPositionRadius(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        PhysShapeCircle shape,
        Vector2 position,
        float radius,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if ((MathHelper.CloseTo(shape.Radius, radius) && shape.Position.EqualsApprox(position)) ||
            !Resolve(uid, ref manager, ref body, ref xform))
            return;

        shape.Position = position;
        shape.Radius = radius;

        if (body.CanCollide &&
            TryComp<BroadphaseComponent>(xform.Broadphase?.Uid, out var broadphase) &&
            TryComp<PhysicsMapComponent>(xform.MapUid, out var physicsMap))
        {
            _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
        }

        Dirty(uid, manager);
    }

    public void SetPosition(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        PhysShapeCircle circle,
        Vector2 position,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if (circle.Position.EqualsApprox(position) || !Resolve(uid, ref manager, ref body, ref xform))
            return;

        circle.Position = position;

        if (body.CanCollide &&
            TryComp<BroadphaseComponent>(xform.Broadphase?.Uid, out var broadphase) &&
            TryComp<PhysicsMapComponent>(xform.MapUid, out var physicsMap))
        {
            _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
        }

        Dirty(uid, manager);
    }

    #endregion

    #region Edge

    public void SetVertices(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        EdgeShape edge,
        Vector2 vertex0,
        Vector2 vertex1,
        Vector2 vertex2,
        Vector2 vertex3,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref manager, ref body, ref xform))
            return;

        edge.Vertex0 = vertex0;
        edge.Vertex1 = vertex1;
        edge.Vertex2 = vertex2;
        edge.Vertex3 = vertex3;

        if (body.CanCollide &&
            TryComp<BroadphaseComponent>(xform.Broadphase?.Uid, out var broadphase) &&
            TryComp<PhysicsMapComponent>(xform.MapUid, out var physicsMap))
        {
            _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
        }

        _fixtures.FixtureUpdate(uid, manager: manager, body: body);
    }

    #endregion

    #region Polygon

    public void SetVertices(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        PolygonShape poly,
        Vector2[] vertices,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref manager, ref body, ref xform))
            return;

        poly.Set(vertices, vertices.Length);

        if (body.CanCollide &&
            TryComp<BroadphaseComponent>(xform.Broadphase?.Uid, out var broadphase) &&
            TryComp<PhysicsMapComponent>(xform.MapUid, out var physicsMap))
        {
            _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
        }

        _fixtures.FixtureUpdate(uid, manager: manager, body: body);
    }

    #endregion
}
