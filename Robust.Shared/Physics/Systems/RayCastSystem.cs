using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Systems;

public sealed class RayCastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    internal CastOutput RayCastShape(RayCastInput input, IPhysShape shape, Transform transform)
    {
        var localInput = input;
        localInput.Origin = b2InvTransformPoint( transform, input.Origin );
        localInput.Translation = b2InvRotateVector( transform.Quaternion2D, input.Translation );

        CastOutput output = new();

        switch (shape)
        {
            /*
            case b2_capsuleShape:
                output = b2RayCastCapsule( &localInput, &shape->capsule );
                break;
                */
            case PhysShapeCircle:
                output = b2RayCastCircle( &localInput, &shape->circle );
                break;
            case PolygonShape polyShape:
                break;
            case Polygon:
                output = b2RayCastPolygon( &localInput, &shape->polygon );
                break;
            case b2_segmentShape:
                output = b2RayCastSegment( &localInput, &shape->segment, false );
                break;
            case b2_smoothSegmentShape:
                output = b2RayCastSegment( &localInput, &shape->smoothSegment.segment, true );
                break;
            default:
                return output;
        }

        output.Point = b2TransformPoint( transform, output.Point );
        output.Normal = b2RotateVector( transform.Quaternion2D, output.Normal );
        return output;
    }

    internal CastOutput RayCast(IPhysShape shape, Vector2 origin, Vector2 translation )
    {
        var transform = b2GetOwnerTransform( world, shape );

        // input in local coordinates
        var input = new RayCastInput();
        input.MaxFraction = 1.0f;
        input.Origin = b2InvTransformPoint( transform, origin );
        input.Translation = b2InvRotateVector( transform.q, translation );

        var output = new CastOutput();
        switch (shape)
        {
            /*
            case b2_capsuleShape:
                output = b2RayCastCapsule( &input, &shape->capsule );
                break;
    */

            case PhysShapeCircle circle:
                output = b2RayCastCircle(input, circle);
                break;

            case b2_segmentShape:
                output = b2RayCastSegment( &input, &shape->segment, false );
                break;

            case Polygon poly:
                output = b2RayCastPolygon( &input, &shape->polygon );
                break;

            case b2_smoothSegmentShape:
                output = b2RayCastSegment( &input, &shape->smoothSegment.segment, true );
                break;

            default:
                throw new NotImplementedException();
        }

        if ( output.hit )
        {
            // convert to world coordinates
            output.normal = b2RotateVector( transform.q, output.normal );
            output.point = b2TransformPoint( transform, output.point );
        }
        return output;
    }

    public void RayCast(MapCoordinates coordinates, Vector2 translation)
    {
        // TODO: Get trees in range.
    }

    public void RayCast(Entity<BroadphaseComponent?> grid, Vector2 origin, Vector2 translation, uint collisionMask = uint.MaxValue - 1)
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

        ((B2DynamicTree<FixtureProxy>) grid.Comp.DynamicTree).RayCastNew(input, collisionMask,
            (castInput, proxy, context) =>
            {

                // TODO: Collision check.
                if ( ( shapeFilter.categoryBits & queryFilter.maskBits ) == 0 || ( shapeFilter.maskBits & queryFilter.categoryBits ) == 0 )
                {
                    return input->maxFraction;
                }

                var body = context.Body;
                var transform = _physics.GetPhysicsTransform(context.Entity);
                var relative = Physics.Transform.MulT(transform, broadphaseTransform);

                var output = RayCastShape(input, context.Fixture.Shape, transform);

                if (output.Hit)
                {
                    b2ShapeId id = { shapeId + 1, world->worldId, shape->revision };
                    float fraction = worldContext->fcn( id, output.point, output.normal, output.fraction, worldContext->userContext );
                    worldContext->fraction = fraction;
                    return fraction;
                }

                return input.MaxFraction;
            });
    }
}

internal ref struct RayCastInput
{
    public Vector2 Origin;

    public Vector2 Translation;

    public float MaxFraction;
}

internal ref struct CastOutput
{
    public Vector2 Normal;

    public Vector2 Point;

    public float Fraction;

    public int Iterations;

    public bool Hit;
}
