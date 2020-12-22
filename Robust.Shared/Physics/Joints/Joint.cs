using System;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics.Joints
{
    public abstract class Joint
    {
        // Box2d documentation on its joints at https://box2d.org/documentation/md__d_1__git_hub_box2d_docs_dynamics.html#autotoc_md81

        private float _breakpoint;
        private double _breakpointSquared;

        /// <summary>
        /// Indicate if this join is enabled or not. Disabling a joint
        /// means it is still in the simulation, but inactive.
        /// </summary>
        public bool Enabled = true;

        internal JointEdge EdgeA = new JointEdge();
        internal JointEdge EdgeB = new JointEdge();
        internal bool IslandFlag;

        protected Joint()
        {
            Breakpoint = float.MaxValue;

            //Connected bodies should not collide by default
            CollideConnected = false;
        }

        protected Joint(PhysicsComponent bodyA, PhysicsComponent bodyB) : this()
        {
            //Can't connect a joint to the same body twice.
            Debug.Assert(bodyA != bodyB);

            BodyA = bodyA;
            BodyB = bodyB;
        }

        /// <summary>
        /// Constructor for fixed joint
        /// </summary>
        protected Joint(PhysicsComponent body) : this()
        {
            BodyA = body;
        }

        /// <summary>
        /// Gets or sets the type of the joint.
        /// </summary>
        /// <value>The type of the joint.</value>
        public JointType JointType { get; protected set; }

        /// <summary>
        /// Get the first body attached to this joint.
        /// </summary>
        public PhysicsComponent BodyA { get; internal set; } = default!;

        /// <summary>
        /// Get the second body attached to this joint.
        /// </summary>
        public PhysicsComponent BodyB { get; internal set; } = default!;

        /// <summary>
        /// Get the anchor point on bodyA in world coordinates.
        /// On some joints, this value indicate the anchor point within the world.
        /// </summary>
        public abstract Vector2 WorldAnchorA { get; set; }

        /// <summary>
        /// Get the anchor point on bodyB in world coordinates.
        /// On some joints, this value indicate the anchor point within the world.
        /// </summary>
        public abstract Vector2 WorldAnchorB { get; set; }

        /// <summary>
        /// Set this flag to true if the attached bodies should collide.
        /// </summary>
        public bool CollideConnected { get; set; }

        /// <summary>
        /// The Breakpoint simply indicates the maximum Value the JointError can be before it breaks.
        /// The default value is float.MaxValue, which means it never breaks.
        /// </summary>
        public float Breakpoint
        {
            get { return _breakpoint; }
            set
            {
                _breakpoint = value;
                _breakpointSquared = _breakpoint * _breakpoint;
            }
        }

        /// <summary>
        /// Fires when the joint is broken.
        /// </summary>
        public event Action<Joint, float>? Broke;

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

        /// <summary>
        /// Return true if the joint is a fixed type.
        /// </summary>
        public bool IsFixedType()
        {
            return JointType == JointType.FixedRevolute ||
                   JointType == JointType.FixedDistance ||
                   JointType == JointType.FixedPrismatic ||
                   JointType == JointType.FixedLine ||
                   JointType == JointType.FixedMouse ||
                   JointType == JointType.FixedAngle ||
                   JointType == JointType.FixedFriction;
        }

        internal abstract void InitVelocityConstraints(ref SolverData data);

        internal void Validate(float invDt)
        {
            if (!Enabled)
                return;

            float jointErrorSquared = GetReactionForce(invDt).LengthSquared;

            if (Math.Abs(jointErrorSquared) <= _breakpointSquared)
                return;

            Enabled = false;

            Broke?.Invoke(this, (float)Math.Sqrt(jointErrorSquared));
        }

        internal abstract void SolveVelocityConstraints(ref SolverData data);

        /// <summary>
        /// Solves the position constraints.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>returns true if the position errors are within tolerance.</returns>
        internal abstract bool SolvePositionConstraints(ref SolverData data);
    }

    public enum JointType : byte
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

    public enum LimitState : byte
    {
        Inactive,
        AtLower,
        AtUpper,
        Equal,
    }
}
