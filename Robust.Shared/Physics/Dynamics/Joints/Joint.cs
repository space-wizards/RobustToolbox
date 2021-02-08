using System;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
    public enum JointType : ushort
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

        // Sloth note: I Removed FPE's fixed joints
    }

    public enum LimitState : byte
    {
        Inactive,
        AtLower,
        AtUpper,
        Equal,
    }

    [Serializable, NetSerializable]
    public abstract class Joint : IExposeData
    {
        /// <summary>
        /// Indicate if this join is enabled or not. Disabling a joint
        /// means it is still in the simulation, but inactive.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;

        [NonSerialized] internal JointEdge EdgeA = new();
        [NonSerialized] internal JointEdge EdgeB = new();

        /// <summary>
        ///     Has this joint already been added to an island.
        /// </summary>
        [NonSerialized] internal bool IslandFlag;

        /// <summary>
        ///     Gets or sets the type of the joint.
        /// </summary>
        /// <value>The type of the joint.</value>
        public abstract JointType JointType { get; }

        /// <summary>
        ///     Get the first body attached to this joint.
        /// </summary>
        [field:NonSerialized] public PhysicsComponent BodyA { get; internal set; }

        public EntityUid BodyAUid { get; internal set; }

        /// <summary>
        ///     Get the second body attached to this joint.
        /// </summary>
        [field:NonSerialized] public PhysicsComponent BodyB { get; internal set; }

        public EntityUid BodyBUid { get; internal set; }

        /// <summary>
        /// Get the anchor point on bodyA in world coordinates.
        /// On some joints, this value indicate the anchor point within the world.
        /// </summary>
        public abstract Vector2 WorldAnchorA { get; set; }

        /// <summary>
        ///     Get the anchor point on bodyB in world coordinates.
        ///     On some joints, this value indicate the anchor point within the world.
        /// </summary>
        public abstract Vector2 WorldAnchorB { get; set; }

        /// <summary>
        ///     Set this flag to true if the attached bodies should collide.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CollideConnected { get; set; }

        /// <summary>
        ///     The Breakpoint simply indicates the maximum Value the JointError can be before it breaks.
        ///     The default value is float.MaxValue, which means it never breaks.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Breakpoint
        {
            get => _breakpoint;
            set
            {
                if (MathHelper.CloseTo(_breakpoint, value)) return;
                _breakpoint = value;
                _breakpointSquared = _breakpoint * _breakpoint;
            }
        }

        private float _breakpoint;
        [NonSerialized] private double _breakpointSquared;

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Enabled, "enabled", true);
            // TODO: Later nerd
            // serializer.DataField(this, x => x.BodyA, "bodyA", EntityUid.Invalid);
            // serializer.DataField(this, x => x.BodyB, "bodyB", Ent);
            serializer.DataField(this, x => x.CollideConnected, "collideConnected", false);
        }

        protected Joint(PhysicsComponent bodyA, PhysicsComponent bodyB)
        {
            //Can't connect a joint to the same body twice.
            Debug.Assert(bodyA != bodyB);

            BodyA = bodyA;
            BodyB = bodyB;
        }

        /// <summary>
        /// Get the reaction force on body at the joint anchor in Newtons.
        /// </summary>
        /// <param name="invDt">The inverse delta time.</param>
        public abstract Vector2 GetReactionForce(float invDt);

        /// <summary>
        /// Get the reaction torque on the body at the joint anchor in N*m.
        /// </summary>
        /// <param name="invDt">The inverse delta time.</param>
        public abstract float GetReactionTorque(float invDt);

        protected void WakeBodies()
        {
            if (BodyA != null)
                BodyA.Awake = true;

            if (BodyB != null)
                BodyB.Awake = true;
        }

        internal abstract void InitVelocityConstraints(SolverData data);

        internal void Validate(float invDt)
        {
            if (!Enabled)
                return;

            float jointErrorSquared = GetReactionForce(invDt).LengthSquared;

            if (MathF.Abs(jointErrorSquared) <= _breakpointSquared)
                return;

            Enabled = false;

            // BodyA
            /* TODO: Dis, just use comp messages and a system message
            if (Broke != null)
                Broke(this, MathF.Sqrt(jointErrorSquared));
                */
        }

        internal abstract void SolveVelocityConstraints(SolverData data);

        /// <summary>
        /// Solves the position constraints.
        /// </summary>
        /// <returns>returns true if the position errors are within tolerance.</returns>
        internal abstract bool SolvePositionConstraints(SolverData data);
    }
}
