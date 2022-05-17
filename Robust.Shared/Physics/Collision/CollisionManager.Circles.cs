using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
    public void CollideCircles(ref Manifold manifold, PhysShapeCircle circleA, in Transform xfA,
        PhysShapeCircle circleB, in Transform xfB)
    {
        manifold.PointCount = 0;

        Vector2 pA = Transform.Mul(xfA, circleA.Position);
        Vector2 pB = Transform.Mul(xfB, circleB.Position);

        Vector2 d = pB - pA;
        float distSqr = Vector2.Dot(d, d);
        float radius = circleA.Radius + circleB.Radius;
        if (distSqr > radius * radius)
        {
            return;
        }

        manifold.Type = ManifoldType.Circles;
        manifold.LocalPoint = circleA.Position;
        manifold.LocalNormal = Vector2.Zero;
        manifold.PointCount = 1;

        ref var p0 = ref manifold.Points[0];

        p0.LocalPoint = Vector2.Zero; // Also here
        p0.Id.Key = 0;
    }
}
