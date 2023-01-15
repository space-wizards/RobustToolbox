using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    private bool _convexHulls;

    public void SetRadius(
        EntityUid uid,
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
            _lookup.DestroyProxies(fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(xform, fixture);
        }

        Dirty(manager);
    }

    #region Circle

    public void SetPositionRadius(
        EntityUid uid,
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
            _lookup.DestroyProxies(fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(xform, fixture);
        }

        Dirty(manager);
    }

    public void SetPosition(
        EntityUid uid,
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
            _lookup.DestroyProxies(fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(xform, fixture);
        }

        Dirty(manager);
    }

    #endregion

    #region Edge

    public void SetVertices(
        EntityUid uid,
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
            _lookup.DestroyProxies(fixture, xform, broadphase, physicsMap);
            _lookup.CreateProxies(xform, fixture);
        }

        Dirty(manager);
    }

    #endregion

    #region Polygon

    public void SetVertices(
        EntityUid uid,
        Fixture fixture,
        PolygonShape poly,
        Vector2[] vertices,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null,
        TransformComponent? xform = null)
    {
        if (vertices.Length > PhysicsConstants.MaxPolygonVertices)
        {
            throw new InvalidOperationException(
                $"Tried to set too many vertices of {vertices.Length} for {ToPrettyString(uid)}!");
        }

        var vertexCount = vertices.Length;

        if (_convexHulls)
        {
            //FPE note: This check is required as the GiftWrap algorithm early exits on triangles
            //So instead of giftwrapping a triangle, we just force it to be clock wise.
            if (vertexCount <= 3)
                poly.Vertices = Vertices.ForceCounterClockwise(vertices.AsSpan());
            else
                poly.Vertices = GiftWrap.SetConvexHull(vertices.AsSpan());
        }
        else
        {
            Array.Resize(ref poly.Vertices, vertexCount);

            for (var i = 0; i < vertices.Length; i++)
            {
                poly.Vertices[i] = vertices[i];
            }
        }

        // Convex hull may prune some vertices hence the count may change by this point.
        vertexCount = poly.Vertices.Length;

        Array.Resize(ref poly.Normals, vertexCount);

        // Compute normals. Ensure the edges have non-zero length.
        for (var i = 0; i < vertexCount; i++)
        {
            var next = i + 1 < vertexCount ? i + 1 : 0;
            var edge = poly.Vertices[next] - poly.Vertices[i];
            DebugTools.Assert(edge.LengthSquared > float.Epsilon * float.Epsilon);

            //FPE optimization: Normals.Add(MathHelper.Cross(edge, 1.0f));
            var temp = new Vector2(edge.Y, -edge.X);
            poly.Normals[i] = temp.Normalized;
        }

        poly.Centroid = ComputeCentroid(poly.Vertices, vertexCount);
    }

    private Vector2 ComputeCentroid(Vector2[] vs, int count)
    {
        DebugTools.Assert(count >= 3);

        var c = new Vector2(0.0f, 0.0f);
        float area = 0.0f;

        // Get a reference point for forming triangles.
        // Use the first vertex to reduce round-off errors.
        var s = vs[0];

        const float inv3 = 1.0f / 3.0f;

        for (var i = 0; i < count; ++i)
        {
            // Triangle vertices.
            var p1 = vs[0] - s;
            var p2 = vs[i] - s;
            var p3 = i + 1 < count ? vs[i+1] - s : vs[0] - s;

            var e1 = p2 - p1;
            var e2 = p3 - p1;

            float D = Vector2.Cross(e1, e2);

            float triangleArea = 0.5f * D;
            area += triangleArea;

            // Area weighted centroid
            c += (p1 + p2 + p3) * triangleArea * inv3;
        }

        // Centroid
        DebugTools.Assert(area > float.Epsilon);
        c = c * (1.0f / area) + s;
        return c;
    }

    #endregion
}
