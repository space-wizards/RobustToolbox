/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
    [DataDefinition]
    public abstract class Joint : IEquatable<Joint>
    {
        /// <summary>
        /// Network identifier of this joint.
        /// </summary>
        [ViewVariables]
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Indicate if this joint is enabled or not. Disabling a joint
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

        [DataField("enabled")]
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

                if (!_collideConnected)
                    EntitySystem.Get<SharedPhysicsSystem>().FilterContactsForJoint(this);

                Dirty();
            }
        }

        [DataField("collideConnected")]
        private bool _collideConnected = true;

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

        // TODO: Later nerd
        // serializer.DataField(this, x => x.BodyA, "bodyA", EntityUid.Invalid);
        // serializer.DataField(this, x => x.BodyB, "bodyB", Ent);

        protected void Dirty()
        {
            BodyA.Dirty();
            BodyB.Dirty();
        }

        public virtual void DebugDraw(DebugDrawingHandle handle, in Box2 worldViewport) {}

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
            BodyA.Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new JointBreakMessage(this, MathF.Sqrt(jointErrorSquared)));
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

        public sealed class JointBreakMessage : EntityEventArgs
        {
            public Joint Joint { get; }
            public float JointError { get; }

            public JointBreakMessage(Joint joint, float jointError)
            {
                Joint = joint;
                JointError = jointError;
            }
        }
    }
}
