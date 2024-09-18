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

namespace Robust.Shared.Physics.Systems;

public sealed partial class RayCastSystem : EntitySystem
{
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    #region RayCast

    private record struct RayCastQueryState
    {
        public RayCastSystem System;
        public SharedPhysicsSystem Physics;

        public Transform BroadphaseTransform;
        public uint CollisionMask;
        public Vector2 Origin;
        public Vector2 Translation;
    }

    /// <summary>
    /// Returns the closest entity hit.
    /// </summary>
    public void RayCastClosest(Vector2 origin, Vector2 translation, QueryFilter filter)
    {

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

    private void RayCast(Entity<BroadphaseComponent?> grid, ref RayCastQueryState state, Vector2 origin, Vector2 translation, uint collisionMask = uint.MaxValue - 1)
    {
        if (!Resolve(grid.Owner, ref grid.Comp))
            return;

        var input = new RayCastInput()
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1f,
        };

        state.BroadphaseTransform = _physics.GetPhysicsTransform(grid);

        grid.Comp.DynamicTree.Tree.RayCastNew(input, collisionMask, ref state, static (
            RayCastInput castInput,
            DynamicTree.Proxy proxy,
            FixtureProxy context,
            ref RayCastQueryState queryState) =>
        {
            // TODO: Collision check.
            if ((shapeFilter.categoryBits & queryFilter.maskBits ) == 0 || ( shapeFilter.maskBits & queryFilter.categoryBits ) == 0 )
            {
                return castInput.MaxFraction;
            }

            var body = context.Body;
            var localTransform = queryState.Physics.GetLocalPhysicsTransform(context.Entity);

            var output = queryState.System.RayCastShape(castInput, context.Fixture.Shape, localTransform);

            if (output.Hit)
            {
                float fraction = worldContext->fcn( id, output.Point, output.Normal, output.Fraction, worldContext->userContext);
                worldContext->fraction = fraction;
                return fraction;
            }

            return castInput.MaxFraction;
        });
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
        b2World* world = b2GetWorldFromId( worldId );
        B2_ASSERT( world->locked == false );
        if ( world->locked )
        {
            return;
        }

        B2_ASSERT( b2Vec2_IsValid( originTransform.p ) );
        B2_ASSERT( b2Rot_IsValid( originTransform.q ) );
        B2_ASSERT( b2Vec2_IsValid( translation ) );

        b2ShapeCastInput input;
        input.points[0] = b2TransformPoint( originTransform, circle->center );
        input.count = 1;
        input.radius = circle->radius;
        input.translation = translation;
        input.maxFraction = 1.0f;

        WorldRayCastContext worldContext = { world, fcn, filter, 1.0f, context };

        for ( int i = 0; i < b2_bodyTypeCount; ++i )
        {
            b2DynamicTree_ShapeCast( world->broadPhase.trees + i, &input, filter.maskBits, ShapeCastCallback, &worldContext );

            if ( worldContext.fraction == 0.0f )
            {
                return;
            }

            input.maxFraction = worldContext.fraction;
        }
    }

    /// <summary>
    /// Cast a polygon through the world. Similar to a cast ray except that a polygon is cast instead of a point.
    /// </summary>
    public void CastPolygon(
        PolygonShape polygon,
        Transform originTransform,
        Vector2 translation,
        QueryFilter filter,
        CastResult fcn)
    {
        b2World* world = b2GetWorldFromId( worldId );
        B2_ASSERT( world->locked == false );
        if ( world->locked )
        {
            return;
        }

        B2_ASSERT( b2Vec2_IsValid( originTransform.p ) );
        B2_ASSERT( b2Rot_IsValid( originTransform.q ) );
        B2_ASSERT( b2Vec2_IsValid( translation ) );

        b2ShapeCastInput input;
        for ( int i = 0; i < polygon->count; ++i )
        {
            input.points[i] = b2TransformPoint( originTransform, polygon->vertices[i] );
        }
        input.count = polygon->count;
        input.radius = polygon->radius;
        input.translation = translation;
        input.maxFraction = 1.0f;

        WorldRayCastContext worldContext = { world, fcn, filter, 1.0f, context };

        for ( int i = 0; i < b2_bodyTypeCount; ++i )
        {
            b2DynamicTree_ShapeCast( world->broadPhase.trees + i, &input, filter.maskBits, ShapeCastCallback, &worldContext );

            if ( worldContext.fraction == 0.0f )
            {
                return;
            }

            input.maxFraction = worldContext.fraction;
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

    internal CastOutput RayCast(IPhysShape shape, Vector2 origin, Vector2 translation )
    {
        var transform = b2GetOwnerTransform( world, shape );

        // input in local coordinates
        var input = new RayCastInput
        {
            MaxFraction = 1.0f,
            Origin = Physics.Transform.InvTransformPoint(transform, origin),
            Translation = Quaternion2D.InvRotateVector(transform.q, translation)
        };

        CastOutput output;
        switch (shape)
        {
            case PhysShapeCircle circle:
                output = RayCastCircle(input, circle);
                break;
            case EdgeShape edge:
                output = RayCastSegment(input, edge, false);
                break;
            case Polygon poly:
                output = RayCastPolygon(input, poly);
                break;
            case PolygonShape pShape:
                output = RayCastPolygon(input, (Polygon) pShape);
                break;
            default:
                throw new NotImplementedException();
        }

        if (output.Hit)
        {
            // convert to world coordinates
            output.Normal = Quaternion2D.RotateVector(transform.q, output.Normal);
            output.Point = Physics.Transform.TransformPoint(transform, output.Point);
        }
        return output;
    }

    #endregion
}

internal ref struct ShapeCastPairInput
{
    public DistanceProxy ProxyA;
    public DistanceProxy ProxyB;
    public Transform TransformA;
    public Transform TransformB;
    public Vector2 TranslationB;

    /// <summary>
    /// The fraction of the translation to consider, typically 1
    /// </summary>
    public float MaxFraction;
}

internal ref struct ShapeCastInput
{
    /// A point cloud to cast
    public Vector2[] Points;

    /// The number of points
    public int Count;

    /// The radius around the point cloud
    public float Radius;

    /// The translation of the shape cast
    public Vector2 Translation;

    /// The maximum fraction of the translation to consider, typically 1
    public float MaxFraction;
}

internal ref struct RayCastInput
{
    public Vector2 Origin;

    public Vector2 Translation;

    public float MaxFraction;

    public bool IsValidRay()
    {
        bool isValid = Origin.IsValid() && Translation.IsValid() && MaxFraction.IsValid() &&
                       0.0f <= MaxFraction && MaxFraction < float.MaxValue;
        return isValid;
    }
}

internal ref struct CastOutput
{
    public Vector2 Normal;

    public Vector2 Point;

    public float Fraction;

    public int Iterations;

    public bool Hit;
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
public delegate float CastResult(FixtureProxy proxy, Vector2 point, Vector2 normal, float fraction);
