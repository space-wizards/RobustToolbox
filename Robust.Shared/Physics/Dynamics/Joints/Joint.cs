/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Joints
{
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
    public abstract class Joint : IExposeData, IEquatable<Joint>
    {
        /// <summary>
        /// Indicate if this join is enabled or not. Disabling a joint
        /// means it is still in the simulation, but inactive.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;
                Dirty();
            }
        }

        private bool _enabled = true;

        [NonSerialized] internal JointEdge EdgeA = new();
        [NonSerialized] internal JointEdge EdgeB = new();

        /// <summary>
        ///     Has this joint already been added to an island.
        /// </summary>
        [NonSerialized] internal bool IslandFlag;

        // For some reason in FPE this is settable?
        /// <summary>
        ///     Gets the type of the joint.
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
        public bool CollideConnected
        {
            get => _collideConnected;
            set
            {
                if (_collideConnected == value) return;
                _collideConnected = value;
                Dirty();
            }
        }

        private bool _collideConnected;

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
                Dirty();
            }
        }

        private float _breakpoint = float.MaxValue;
        private double _breakpointSquared = Double.MaxValue;

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Enabled, "enabled", true);
            // TODO: Later nerd
            // serializer.DataField(this, x => x.BodyA, "bodyA", EntityUid.Invalid);
            // serializer.DataField(this, x => x.BodyB, "bodyB", Ent);
            serializer.DataField(this, x => x.CollideConnected, "collideConnected", false);
        }

        protected void Dirty()
        {
            BodyA.Dirty();
            BodyB.Dirty();
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

        public bool Equals(Joint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Enabled == other.Enabled &&
                   JointType == other.JointType &&
                   BodyAUid.Equals(other.BodyAUid) &&
                   BodyBUid.Equals(other.BodyBUid) &&
                   CollideConnected == other.CollideConnected &&
                   MathHelper.CloseTo(_breakpoint, other._breakpoint);
        }
    }
}
