using System;
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

    #region RayCast

    /// <summary>
    /// Tells the callback we want every entity.
    /// </summary>
    private static float RayCastAllCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, WorldRayCastContext context)
    {
        context.Result.Results.Add(new RayHit()
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
    private static float RayCastClosestCallback(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction, WorldRayCastContext context)
    {
        var add = false;

        if (context.Result.Results.Count > 0)
        {
            if (context.Result.Results[0].Fraction > fraction)
            {
                add = true;
                context.Result.Results.Clear();
            }
        }
        else
        {
            add = true;
        }

        if (add)
        {
            context.Result.Results.Add(new RayHit()
            {
                Fraction = fraction,
                Normal = normal,
                Point = point,
                Proxy = proxy,
            });
        }

        return fraction;
    }

    /// <summary>
    /// Casts a ray against a broadphase. Requires you to handle the <see cref="CastResult"/> callback yourself
    /// </summary>
    public void CastRay(Entity<BroadphaseComponent?> entity, Vector2 origin, Vector2 translation, QueryFilter filter, CastResult callback)
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
            fcn = callback,
            Filter = filter,
            Fraction = 1f,
            Physics = _physics,
            System = this,
            Result = new RayResult(),
        };

        entity.Comp.DynamicTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);

        if (worldContext.Fraction == 0f)
            return;

        input.MaxFraction = worldContext.Fraction;
        entity.Comp.StaticTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
    }

    /// <summary>
    /// Returns the closest entity hit.
    /// </summary>
    public RayResult CastRayClosest(Vector2 origin, Vector2 translation, QueryFilter filter)
    {
        var result = new RayResult();

        DebugTools.Assert(origin.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new RayCastInput()
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1.0f
        };

        var worldContext = new WorldRayCastContext
        {
            fcn = RayCastClosestCallback,
            Filter = filter,
            Fraction = 1f,
            Physics = _physics,
            System = this,
            Result = result,
        };

        entity.Comp.DynamicTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);

        if (worldContext.Fraction != 0f)
        {
            input.MaxFraction = worldContext.Fraction;
            entity.Comp.StaticTree.Tree.RayCastNew(input, filter.MaskBits, ref worldContext, RayCastCallback);
        }

        return result;
    }

    public void RayCast(MapCoordinates coordinates, Vector2 translation, uint collisionMask = uint.MaxValue - 1)
    {
        var end = coordinates.Position + translation;
        var aabb = new Box2(Vector2.Min(coordinates.Position, end), Vector2.Max(coordinates.Position, end));

        var state = new RayCastQueryState()
        {
            System = this,
            Physics = _physics,

            CollisionMask = collisionMask,
            Origin = coordinates.Position,
            Translation = translation,
        };

        _broadphase.GetBroadphases(coordinates.MapId,
            aabb, ref state,
            static (Entity<BroadphaseComponent> entity, ref RayCastQueryState state) =>
            {
                var transform = state.Physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.InvTransformPoint(transform, state.Origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, state.Origin + state.Translation);

                state.System.RayCast((entity.Owner, entity.Comp), localOrigin, localTranslation, collisionMask: state.CollisionMask);
            });
    }

    public void RayCast(
        Entity<BroadphaseComponent?> grid,
        Vector2 origin,
        Vector2 translation,
        uint collisionMask = uint.MaxValue - 1)
    {
        if (!Resolve(grid.Owner, ref grid.Comp))
            return;

        var state = new RayCastQueryState()
        {
            System = this,
            Origin = origin,
            Translation = translation,
            Physics = _physics,
            CollisionMask = collisionMask,
        };

        RayCast(grid, ref state, origin, translation, collisionMask);
    }

    #endregion

    #region ShapeCast

    /// <summary>
    /// Convenience method for shape casts; only supports shapes with area.
    /// </summary>
    public void CastShape(
        IPhysShape shape,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult fcn)
    {
        switch (shape)
        {
            case PhysShapeCircle circle:
                CastCircle(circle, originTransform, translation, filter, fcn);
                break;
            case PolygonShape poly:
                CastPolygon(poly, originTransform, translation, filter, fcn);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Cast a circle through the world. Similar to a cast ray except that a circle is cast instead of a point.
    /// </summary>
    public void CastCircle(
        PhysShapeCircle circle,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult fcn)
    {
        DebugTools.Assert(originTransform.Position.IsValid());
        DebugTools.Assert(originTransform.Quaternion2D.IsValid());
        DebugTools.Assert(translation.IsValid());

        var input = new ShapeCastInput()
        {
            Points = new Vector2[1],
            Count = 1,
            Radius = circle.Radius,
            Translation = translation,
            MaxFraction = 1f,
        };

        input.Points[0] = Physics.Transform.TransformPoint(originTransform, circle.Position);

        WorldRayCastContext worldContext = { world, fcn, filter, 1.0f, context };

        for ( int i = 0; i < b2_bodyTypeCount; ++i )
        {
            b2DynamicTree_ShapeCast( world->broadPhase.trees + i, &input, filter.maskBits, ShapeCastCallback, &worldContext );

            if ( worldContext.Fraction == 0.0f )
            {
                return;
            }

            input.MaxFraction = worldContext.Fraction;
        }
    }

    /// <summary>
    /// Cast a polygon through the world. Similar to a cast ray except that a polygon is cast instead of a point.
    /// </summary>
    public void CastPolygon(
        MapId mapId,
        PolygonShape polygon,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult fcn)
    {
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
            Physics = _physics,
            System = this,
            fcn = fcn,
            Filter = filter,
            Fraction = 1f,
            Result = new RayResult(),
        };

        var startAabb = polygon.ComputeAABB(originTransform, 0);
        var endAabb = polygon.ComputeAABB(new Transform(originTransform.Position), 1);

        _broadphase.GetBroadphases(mapId);

        for ( int i = 0; i < b2_bodyTypeCount; ++i )
        {
            b2DynamicTree_ShapeCast( world->broadPhase.trees + i, &input, filter.maskBits, ShapeCastCallback, &worldContext );

            if ( worldContext.Fraction == 0.0f )
            {
                return;
            }

            input.MaxFraction = worldContext.Fraction;
        }
    }

    private CastOutput RayCastShape(RayCastInput input, IPhysShape shape, Transform transform)
    {
        var localInput = input;
        localInput.Origin = Physics.Transform.InvTransformPoint(transform, input.Origin);
        localInput.Translation = Quaternion2D.InvRotateVector(transform.Quaternion2D, input.Translation);

        CastOutput output = new();

        switch (shape)
        {
            /*
            case b2_capsuleShape:
                output = b2RayCastCapsule( &localInput, &shape->capsule );
                break;
                */
            case PhysShapeCircle circle:
                output = RayCastCircle(localInput, circle);
                break;
            case PolygonShape polyShape:
                output = RayCastPolygon(localInput, (Polygon) polyShape);
                break;
            case Polygon poly:
                output = RayCastPolygon(localInput, poly);
                break;
            default:
                return output;
        }

        output.Point = Physics.Transform.TransformPoint(transform, output.Point);
        output.Normal = Quaternion2D.RotateVector(transform.Quaternion2D, output.Normal);
        return output;
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
