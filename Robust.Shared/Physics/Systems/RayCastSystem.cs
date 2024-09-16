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

    private record struct RayCastQueryState
    {
        public RayCastSystem System;
        public SharedPhysicsSystem Physics;

        public uint CollisionMask;
        public Vector2 Origin;
        public Vector2 Translation;
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

                state.System.RayCast((entity.Owner, entity.Comp), localOrigin, localTranslation);
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
        };

        RayCast(grid, state, origin, translation, collisionMask);
    }

    private void RayCast(Entity<BroadphaseComponent?> grid, RayCastQueryState state, Vector2 origin, Vector2 translation, uint collisionMask = uint.MaxValue - 1)
    {
        if (!Resolve(grid.Owner, ref grid.Comp))
            return;

        var input = new RayCastInput()
        {
            Origin = origin,
            Translation = translation,
            MaxFraction = 1f,
        };

        var broadphaseTransform = _physics.GetPhysicsTransform(grid);

        grid.Comp.DynamicTree.Tree.RayCastNew(input, collisionMask,
            static (castInput, proxy, context) =>
            {
                // TODO: Collision check.
                if ((shapeFilter.categoryBits & queryFilter.maskBits ) == 0 || ( shapeFilter.maskBits & queryFilter.categoryBits ) == 0 )
                {
                    return castInput.MaxFraction;
                }

                var body = context.Body;
                var transform = _physics.GetPhysicsTransform(context.Entity);
                var relative = Physics.Transform.MulT(transform, broadphaseTransform);

                var output = RayCastShape(castInput, context.Fixture.Shape, transform);

                if (output.Hit)
                {
                    float fraction = worldContext->fcn( id, output.Point, output.Normal, output.Fraction, worldContext->userContext);
                    worldContext->fraction = fraction;
                    return fraction;
                }

                return castInput.MaxFraction;
            });
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
}

internal ref struct ShapeCastPairInput
{
    public DistanceProxy ProxyA; ///< The proxy for shape A
    public DistanceProxy ProxyB; ///< The proxy for shape B
    public Transform TransformA; ///< The world transform for shape A
    public Transform TransformB; ///< The world transform for shape B
    public Vector2 TranslationB;	///< The translation of shape B
    public float MaxFraction;		///< The fraction of the translation to consider, typically 1
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
