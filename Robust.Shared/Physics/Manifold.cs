using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public readonly struct Manifold
    {
        public readonly IPhysicsComponent A;
        public readonly IPhysicsComponent B;

        public readonly Vector2 Normal;
        public readonly bool Hard;

        public Vector2 RelativeVelocity => B.LinearVelocity - A.LinearVelocity;

        public bool Unresolved => Vector2.Dot(RelativeVelocity, Normal) < 0 && Hard;

        public Manifold(IPhysicsComponent a, IPhysicsComponent b, bool hard)
        {
            A = a;
            B = b;
            Normal = PhysicsManager.CalculateNormal(a, b);
            Hard = hard;
        }
    }
}
