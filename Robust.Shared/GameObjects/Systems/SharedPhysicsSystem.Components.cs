using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects;

public partial class SharedPhysicsSystem
{
    public void SetLinearVelocity(PhysicsComponent body, Vector2 velocity)
    {
        if (body.BodyType == BodyType.Static) return;

        if (Vector2.Dot(velocity, velocity) > 0.0f)
            body.Awake = true;

        if (body._linearVelocity.EqualsApprox(velocity, 0.0001f))
            return;

        body._linearVelocity = velocity;
        body.Dirty(EntityManager);
    }

    public Box2 GetWorldAABB(PhysicsComponent body, TransformComponent xform, EntityQuery<TransformComponent> xforms, EntityQuery<FixturesComponent> fixtures)
    {
        var (worldPos, worldRot) = xform.GetWorldPositionRotation(xforms);

        var transform = new Transform(worldPos, (float) worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in fixtures.GetComponent(body.Owner).Fixtures.Values)
        {
            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        return bounds;
    }
}
