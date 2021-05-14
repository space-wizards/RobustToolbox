using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    // better than calcium
    public static class JointHelpers
    {
        public static DistanceJoint CreateDistanceJoint(this PhysicsComponent bodyA, PhysicsComponent bodyB)
        {
            var joint = new DistanceJoint(bodyA, bodyB, Vector2.Zero, Vector2.Zero);
            bodyA.AddJoint(joint);
            return joint;
        }
    }
}
