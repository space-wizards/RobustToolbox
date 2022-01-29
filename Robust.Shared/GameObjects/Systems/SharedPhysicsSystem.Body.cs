using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public partial class SharedPhysicsSystem
{
    public Vector2 GetLinearVelocityFromWorldPoint(PhysicsComponent body, Vector2 worldPoint)
    {
        var transform = body.GetTransform();
        var localPoint = Physics.Transform.MulT(transform, worldPoint);

        return GetLinearVelocityFromLocalPoint(body, localPoint);
    }

    public Vector2 GetLinearVelocityFromLocalPoint(PhysicsComponent body, Vector2 localPoint)
    {
        return body.LinearVelocity + Vector2.Cross(body.AngularVelocity, localPoint);
    }
}
