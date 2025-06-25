using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public sealed partial class RayCastSystem : EntitySystem
{
    /*
     * A few things to keep in mind with the below:
     * - Raycasts are done relative to the corresponding broadphases.
     * - The raycast results need to be transformed into Map terms.
     * - If you wish to add more helper methods make a new partial and dump them there and have them call the below methods.
     */

    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly RayComparer _rayComparer = new();

    #region RayCast

    private sealed class RayComparer : IComparer<RayHit>
    {
        public int Compare(RayHit x, RayHit y)
        {
            return x.Fraction.CompareTo(y.Fraction);
        }
    }

    private void AdjustResults(ref RayResult result, int index, Transform xf)
    {
        for (var i = index; i < result.Results.Count; i++)
        {
            result.Results[i].Point = Physics.Transform.Mul(xf, result.Results[i].Point);
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
    [Pure]
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
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin + tuple.input.Translation) - localOrigin;
                var oldIndex = tuple.result.Results.Count;
                tuple.system.CastRay((entity.Owner, entity.Comp), ref tuple.result, localOrigin, localTranslation, filter: tuple.filter, sorted: false);
                tuple.system.AdjustResults(ref tuple.result, oldIndex, transform);
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
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.input.Origin + tuple.input.Translation) - localOrigin;

                var oldIndex = tuple.result.Results.Count;
                tuple.system.CastRayClosest((entity.Owner, entity.Comp), ref tuple.result, localOrigin, localTranslation, filter: tuple.filter);
                tuple.system.AdjustResults(ref tuple.result, oldIndex, transform);
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
        QueryFilter filter,
        CastResult callback)
    {
        DebugTools.Assert(originTransform.Position.IsValid());
        DebugTools.Assert(originTransform.Quaternion2D.IsValid());
        DebugTools.Assert(translation.IsValid());

        // Need to get the entire shape AABB to know what broadphases to even query.
        var startAabb = shape.ComputeAABB(originTransform, 0);
        var endAabb = shape.ComputeAABB(new Transform(originTransform.Position + translation, originTransform.Quaternion2D.Angle), 0);
        var aabb = startAabb.Union(endAabb);

        var result = new RayResult();
        var state = (originTransform, translation, shape: shape, filter, result, this, _physics, callback);

        _broadphase.GetBroadphases(mapId, aabb, ref state,
            static (
                Entity<BroadphaseComponent> entity,
                ref (Transform origin, Vector2 translation, IPhysShape shape, QueryFilter filter, RayResult result, RayCastSystem system, SharedPhysicsSystem _physics, CastResult callback
                    ) tuple) =>
            {
                var transform = tuple._physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.MulT(transform, tuple.origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.origin.Position + tuple.translation) - localOrigin.Position;

                var oldIndex = tuple.result.Results.Count;
                tuple.system.CastShape((entity.Owner, entity.Comp), ref tuple.result, tuple.shape, localOrigin, localTranslation, filter: tuple.filter, callback: tuple.callback);
                tuple.system.AdjustResults(ref tuple.result, oldIndex, transform);
            });

        result = state.result;
        return result;
    }

    /// <summary>
    /// Cast on the broadphase.
    /// </summary>
    public void CastShape(
        Entity<BroadphaseComponent?> entity,
        ref RayResult result,
        IPhysShape shape,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult callback)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        switch (shape)
        {
            case PhysShapeCircle circle:
                CastCircle(entity, ref result, circle, originTransform, translation, filter, callback);
                break;
            case SlimPolygon slim:
                CastPolygon(entity, ref result, new PolygonShape(slim), originTransform, translation, filter, callback);
                break;
            case Polygon poly:
                CastPolygon(entity, ref result, new PolygonShape(poly), originTransform, translation, filter, callback);
                break;
            case PolygonShape polygon:
                CastPolygon(entity, ref result, polygon, originTransform, translation, filter, callback);
                break;
            default:
                Log.Error("Tried to shapecast for shape not implemented.");
                DebugTools.Assert(false);
                return;
        }
    }

    public void CastCircle(
        Entity<BroadphaseComponent?> entity,
        ref RayResult result,
        PhysShapeCircle circle,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult callback)
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

        input.Points[0] = Physics.Transform.Mul(originTransform, circle.Position);

        var worldContext = new WorldRayCastContext()
        {
            System = this,
            Physics = _physics,
            Filter = filter,
            Fraction = 1f,
            Result = result,
            fcn = callback,
        };

        entity.Comp.StaticTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        input.MaxFraction = worldContext.Fraction;
        entity.Comp.DynamicTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        result = worldContext.Result;
    }

    public void CastPolygon(
        Entity<BroadphaseComponent?> entity,
        ref RayResult result,
        PolygonShape polygon,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult callback)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        ShapeCastInput input = new()
        {
            Points = new Vector2[polygon.VertexCount],
        };

        for ( int i = 0; i < polygon.VertexCount; ++i )
        {
            input.Points[i] = Physics.Transform.Mul(originTransform, polygon.Vertices[i]);
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
            fcn = callback,
        };

        if ((filter.Flags & QueryFlags.Static) == QueryFlags.Static)
        {
            entity.Comp.StaticTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
            input.MaxFraction = worldContext.Fraction;
        }

        if ((filter.Flags & QueryFlags.Dynamic) == QueryFlags.Dynamic)
        {
            entity.Comp.DynamicTree.Tree.ShapeCast(input, filter.MaskBits, ShapeCastCallback, ref worldContext);
        }

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

    public static readonly RayResult Empty = new();
}

public record struct RayHit(EntityUid Entity, Vector2 LocalNormal, float Fraction)
{
    public readonly EntityUid Entity = Entity;
    public readonly Vector2 LocalNormal = LocalNormal;
    public readonly float Fraction = Fraction;

    // When this point gets added it's in broadphase terms, then the caller handles whether it gets turned into map-terms.

    public Vector2 Point;
}

/// The query filter is used to filter collisions between queries and shapes. For example,
///	you may want a ray-cast representing a projectile to hit players and the static environment
///	but not debris.
/// @ingroup shape
public record struct QueryFilter()
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

    /// <summary>
    /// Return whether to ignore an entity.
    /// </summary>
    public Func<EntityUid, bool>? IsIgnored;

    public QueryFlags Flags = QueryFlags.Dynamic | QueryFlags.Static;
}

/// <summary>
/// Which trees we wish to query.
/// </summary>
[Flags]
public enum QueryFlags : byte
{
    None = 0,

    Dynamic = 1 << 0,

    Static = 1 << 1,

    Sensors = 1 << 2,

    // StaticSundries = 1 << 3,

    // Sundries = 1 << 4,
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
public delegate float CastResult(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, ref RayResult result);
