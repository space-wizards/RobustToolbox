using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal sealed class Joint
    {
        // TODO: Rename flags just to be consistent

        /// <summary>
        ///     Even if a joint is disabled it'll still be in the simulation, just inactive.
        /// </summary>
        internal bool Enabled { get; set; } = true;

        /// <summary>
        ///     Has this joint been added to an island?
        /// </summary>
        internal bool IslandFlag { get; set; }

        internal JointEdge EdgeA { get; set; } = new JointEdge();

        internal JointEdge EdgeB { get; set; } = new JointEdge();

        internal IPhysBody? BodyA { get; set; }

        internal IPhysBody? BodyB { get; set; }

        // TODO: This should probably just be null
        /// <summary>
        ///     Point at which this joint breaks.
        /// </summary>
        internal float BreakPoint { get; set; } = float.MaxValue;

        internal JointType JointType { get; set; }

        /// <summary>
        ///     Should the connected bodies collide with each other.
        /// </summary>
        internal bool CollideConnected { get; set; } = false;

        internal Joint(IPhysBody bodyA, IPhysBody bodyB)
        {
            DebugTools.Assert(bodyA != bodyB);

            BodyA = bodyA;
            BodyB = bodyB;
        }

        /// <summary>
        ///     Fixed joint
        /// </summary>
        /// <param name="body"></param>
        internal Joint(IPhysBody body)
        {
            BodyA = body;
        }
    }

    public enum JointType
    {
        Unknown,
        Revolute,
        Prismatic,
        Distance,
        Pulley,
        //Mouse, <- We have fixed mouse
        Gear,
        Wheel,
        Weld,
        Friction,
        Rope,
        Motor,

        //FPE note: From here on and down, it is only FPE joints
        Angle,
        FixedMouse,
        FixedRevolute,
        FixedDistance,
        FixedLine,
        FixedPrismatic,
        FixedAngle,
        FixedFriction,
    }
}
