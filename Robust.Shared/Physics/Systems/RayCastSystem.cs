using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public sealed partial class RayCastSystem : EntitySystem
{
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly RayComparer _rayComparer = new();

    #region Callbacks

    /// <summary>
    /// Tells the callback we want every entity.
    /// </summary>
    private static float RayCastAllCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, RayResult result)
    {
        result.Results.Add(new RayHit()
        {
            Fraction = fraction,
            Normal = normal,
            Point = point,
            Proxy = proxy,
        });
        return 1f;
    }

    /// <summary>
    /// This just lets the callback continue.
    /// </summary>
    private static float RayCastClosestCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, RayResult result)
    {
        var add = false;

        if (result.Results.Count > 0)
        {
            if (result.Results[0].Fraction > fraction)
            {
                add = true;
                result.Results.Clear();
            }
        }
        else
        {
            add = true;
        }

        if (add)
        {
            result.Results.Add(new RayHit()
            {
                Fraction = fraction,
                Normal = normal,
                Point = point,
                Proxy = proxy,
            });
        }

        return fraction;
    }

    #endregion

    #region RayCast

    private sealed class RayComparer : IComparer<RayHit>
    {
        public int Compare(RayHit x, RayHit y)
        {
            return x.Fraction.CompareTo(y.Fraction);
        }
    }

    /*
     * Raycasts that return all entities sorted.
     */

    /// <summary>
    /// Casts a ray against a broadphase.
    /// </summary>
    public void CastRay(Entity<BroadphaseComponent?> entity, ref RayResult result, Vector2 origin, Vector2 translation, QueryFilter filter, bool sorted = true)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        DebugTools.Assert(origin.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new RayCastInput()
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1f,
        };

        var worldContext = new WorldRayCastContext()
        {
            fcn = RayCastAllCallback,
            Filter = filter,
            Fraction = 1f,
            Physics = _physics,
            System = this,
            Result = result,
        };

        entity.Comp.DynamicTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
        input.MaxFraction = worldContext.Fraction;
        entity.Comp.StaticTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
        result = worldContext.Result;

        if (sorted)
        {
            result.Results.Sort(_rayComparer);
        }
    }

    /// <summary>
    /// Returns all entities hit in order.
    /// </summary>
    public RayResult CastRay(MapId mapId, Vector2 origin, Vector2 translation, QueryFilter filter)
    {
        DebugTools.Assert(origin.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new RayCastInput
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1.0f
        };

        var result = new RayResult();

        var start = origin;
        var end = origin + translation;

        var aabb = new Box2(Vector2.Min(start, end), Vector2.Max(start, end));
        var state = (input, filter, result, this, _physics);

        _broadphase.GetBroadphases(mapId, aabb, ref state,
            static (Entity<BroadphaseComponent> entity, ref (RayCastInput input, QueryFilter filter, RayResult result, RayCastSystem system, SharedPhysicsSystem Physics) tuple) =>
            {
                var transform = tuple.Physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin + tuple.input.Translation);

                tuple.system.CastRay((entity.Owner, entity.Comp), ref tuple.result, localOrigin, localTranslation, filter: tuple.filter, sorted: false);
            });

        result = state.result;
        result.Results.Sort(_rayComparer);
        return result;
    }

    /*
     * Raycasts that only return the closest entity.
     */

    /// <summary>
    /// Casts a ray against a broadphase.
    /// </summary>
    public void CastRayClosest(Entity<BroadphaseComponent?> entity, ref RayResult result, Vector2 origin, Vector2 translation, QueryFilter filter)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        DebugTools.Assert(origin.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new RayCastInput()
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1f,
        };

        var worldContext = new WorldRayCastContext()
        {
            fcn = RayCastClosestCallback,
            Filter = filter,
            Fraction = 1f,
            Physics = _physics,
            System = this,
            Result = result,
        };

        entity.Comp.DynamicTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
        input.MaxFraction = worldContext.Fraction;
        entity.Comp.StaticTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
        result = worldContext.Result;
        DebugTools.Assert(result.Results.Count <= 1);
    }

    /// <summary>
    /// Returns all entities hit in order.
    /// </summary>
    public RayResult CastRayClosest(MapId mapId, Vector2 origin, Vector2 translation, QueryFilter filter)
    {
        DebugTools.Assert(origin.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new RayCastInput
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1.0f
        };

        var result = new RayResult();

        var end = origin + translation;

        var aabb = new Box2(Vector2.Min(origin, end), Vector2.Max(origin, end));
        var state = (input, filter, result, this, _physics);

        _broadphase.GetBroadphases(mapId, aabb, ref state,
            static (Entity<BroadphaseComponent> entity, ref (RayCastInput input, QueryFilter filter, RayResult result, RayCastSystem system, SharedPhysicsSystem _physics) tuple) =>
            {
                var transform = tuple._physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin + tuple.input.Translation);

                tuple.system.CastRayClosest((entity.Owner, entity.Comp), ref tuple.result, localOrigin, localTranslation, filter: tuple.filter);
            });

        result = state.result;
        DebugTools.Assert(result.Results.Count <= 1);
        return result;
    }

    #endregion

    #region ShapeCast

    /// <summary>
    /// Convenience method for shape casts; only supports shapes with area.
    /// </summary>
    public RayResult CastShape(
        MapId mapId,
        IPhysShape shape,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter)
    {
        switch (shape)
        {
            case PhysShapeCircle circle:
                return CastCircle(mapId, circle, originTransform, translation, filter);
            case PolygonShape poly:
                return CastPolygon(mapId, poly, originTransform, translation, filter);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Cast a circle through the world. Similar to a cast ray except that a circle is cast instead of a point.
    /// </summary>
    public RayResult CastCircle(
        MapId mapId,
        PhysShapeCircle circle,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter)
    {
        DebugTools.Assert(originTransform.Position.IsValid());
        DebugTools.Assert(originTransform.Quaternion2D.IsValid());
        DebugTools.Assert(translation.IsValid());

        // Need to get the entire shape AABB to know what broadphases to even query.
        var startAabb = circle.ComputeAABB(originTransform, 0);
        var endAabb = circle.ComputeAABB(new Transform(originTransform.Position + translation, originTransform.Quaternion2D.Angle), 0);
        var aabb = startAabb.Union(endAabb);

        var result = new RayResult();
        var state = (originTransform, translation, shape: circle, filter, result, this, _physics);

        _broadphase.GetBroadphases(mapId, aabb, ref state,
            static (
                Entity<BroadphaseComponent> entity,
                ref (Transform origin, Vector2 translation, PhysShapeCircle shape, QueryFilter filter, RayResult result, RayCastSystem system, SharedPhysicsSystem _physics
                    ) tuple) =>
            {
                var transform = tuple._physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.MulT(transform, tuple.origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.origin.Position + tuple.translation);

                tuple.system.CastCircle((entity.Owner, entity.Comp), ref tuple.result, tuple.shape, localOrigin, localTranslation, filter: tuple.filter);
            });

        result = state.result;
        return result;
    }

    public void CastCircle(
        Entity<BroadphaseComponent?> entity,
        ref RayResult result,
        PhysShapeCircle circle,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        var input = new ShapeCastInput()
        {
            Points = new Vector2[1],
            Count = 1,
            Radius = circle.Radius,
            Translation = translation,
            MaxFraction = 1f,
        };

        input.Points[0] = Physics.Transform.TransformPoint(originTransform, circle.Position);

        var worldContext = new WorldRayCastContext()
        {
            System = this,
            Physics = _physics,
            Filter = filter,
            Fraction = 1f,
            Result = result,
            fcn = RayCastAllCallback,
        };

        entity.Comp.StaticTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        input.MaxFraction = worldContext.Fraction;
        entity.Comp.DynamicTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        result = worldContext.Result;
    }

    /// <summary>
    /// Cast a circle through the world. Similar to a cast ray except that a circle is cast instead of a point.
    /// </summary>
    public RayResult CastPolygon(
        MapId mapId,
        PolygonShape poly,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter)
    {
        DebugTools.Assert(originTransform.Position.IsValid());
        DebugTools.Assert(originTransform.Quaternion2D.IsValid());
        DebugTools.Assert(translation.IsValid());

        // Need to get the entire shape AABB to know what broadphases to even query.
        var startAabb = poly.ComputeAABB(originTransform, 0);
        var endAabb = poly.ComputeAABB(new Transform(originTransform.Position + translation, originTransform.Quaternion2D.Angle), 0);
        var aabb = startAabb.Union(endAabb);

        var result = new RayResult();
        var state = (originTransform, translation, shape: poly, filter, result, this, _physics);

        _broadphase.GetBroadphases(mapId, aabb, ref state,
            static (
                Entity<BroadphaseComponent> entity,
                ref (Transform origin, Vector2 translation, PolygonShape shape, QueryFilter filter, RayResult result, RayCastSystem system, SharedPhysicsSystem _physics
                    ) tuple) =>
            {
                var transform = tuple._physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.MulT(transform, tuple.origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.origin.Position + tuple.translation);

                tuple.system.CastPolygon((entity.Owner, entity.Comp), ref tuple.result, tuple.shape, localOrigin, localTranslation, filter: tuple.filter);
            });

        result = state.result;
        return result;
    }

    public void CastPolygon(
        Entity<BroadphaseComponent?> entity,
        ref RayResult result,
        PolygonShape polygon,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        ShapeCastInput input = new()
        {
            Points = new Vector2[polygon.VertexCount],
        };

        for ( int i = 0; i < polygon.VertexCount; ++i )
        {
            input.Points[i] = Physics.Transform.TransformPoint(originTransform, polygon.Vertices[i]);
        }

        input.Count = polygon.VertexCount;
        input.Radius = polygon.Radius;
        input.Translation = translation;
        input.MaxFraction = 1.0f;

        var worldContext = new WorldRayCastContext()
        {
            System = this,
            Physics = _physics,
            Filter = filter,
            Fraction = 1f,
            Result = result,
            fcn = RayCastAllCallback,
        };

        entity.Comp.StaticTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        input.MaxFraction = worldContext.Fraction;
        entity.Comp.DynamicTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        result = worldContext.Result;
    }

    #endregion
}

/// Result from b2World_RayCastClosest
/// @ingroup world
public record struct RayResult()
{
    public ValueList<RayHit> Results = new();

    public bool Hit => Results.Count > 0;
}

public record struct RayHit()
{
    public FixtureProxy Proxy;
    public Vector2 Point;
    public Vector2 Normal;
    public float Fraction;
}

/// The query filter is used to filter collisions between queries and shapes. For example,
///	you may want a ray-cast representing a projectile to hit players and the static environment
///	but not debris.
/// @ingroup shape
public record struct QueryFilter
{
    /// <summary>
    /// The collision category bits of this query. Normally you would just set one bit.
    /// </summary>
    public long LayerBits;

    /// <summary>
    /// The collision mask bits. This states the shape categories that this
    /// query would accept for collision.
    /// </summary>
    public long MaskBits;
}

/// Prototype callback for ray casts.
/// Called for each shape found in the query. You control how the ray cast
/// proceeds by returning a float:
/// return -1: ignore this shape and continue
/// return 0: terminate the ray cast
/// return fraction: clip the ray to this point
/// return 1: don't clip the ray and continue
/// @param shapeId the shape hit by the ray
/// @param point the point of initial intersection
/// @param normal the normal vector at the point of intersection
/// @param fraction the fraction along the ray at the point of intersection
///	@param context the user context
/// @return -1 to filter, 0 to terminate, fraction to clip the ray for closest hit, 1 to continue
/// @see b2World_CastRay
///	@ingroup world
public delegate float CastResult(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, RayResult result);
