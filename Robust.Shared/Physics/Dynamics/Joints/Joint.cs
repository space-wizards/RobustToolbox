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
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
    public abstract class JointState
    {
        public string ID { get; internal set; } = default!;
        public bool Enabled { get; internal set; }
        public bool CollideConnected { get; internal set; }
        public EntityUid UidA { get; internal set; }
        public EntityUid UidB { get; internal set; }
        public Vector2 LocalAnchorA { get; internal set; }
        public Vector2 LocalAnchorB { get; internal set; }
        public float Breakpoint { get; internal set; }

        public abstract Joint GetJoint();
    }

    [ImplicitDataDefinitionForInheritors]
    public abstract class Joint : IEquatable<Joint>
    {
        /// <summary>
        /// Network identifier of this joint.
        /// </summary>
        [ViewVariables]
        [DataField("id")]
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

        /// <summary>
        ///     Has this joint already been added to an island.
        /// </summary>
        internal bool IslandFlag;

        // For some reason in FPE this is settable?
        /// <summary>
        ///     Gets the type of the joint.
        /// </summary>
        /// <value>The type of the joint.</value>
        public abstract JointType JointType { get; }

        /// <summary>
        ///     Get the first body attached to this joint.
        /// </summary>
        public PhysicsComponent BodyA =>
            IoCManager.Resolve<IEntityManager>().GetComponent<PhysicsComponent>(BodyAUid);

        [DataField("bodyA")]
        public EntityUid BodyAUid { get; init; }

        /// <summary>
        ///     Get the second body attached to this joint.
        /// </summary>
        public PhysicsComponent BodyB =>
            IoCManager.Resolve<IEntityManager>().GetComponent<PhysicsComponent>(BodyBUid);

        [DataField("bodyB")]
        public EntityUid BodyBUid { get; init; }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorA
        {
            get => _localAnchorA;
            set
            {
                if (_localAnchorA.EqualsApprox(value)) return;
                _localAnchorA = value;
                Dirty();
            }
        }

        [DataField("localAnchorA")]
        private Vector2 _localAnchorA;

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalAnchorB
        {
            get => _localAnchorB;
            set
            {
                if (_localAnchorB.EqualsApprox(value)) return;
                _localAnchorB = value;
                Dirty();
            }
        }

        [DataField("localAnchorB")]
        private Vector2 _localAnchorB;

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
                    EntitySystem.Get<SharedJointSystem>().FilterContactsForJoint(this);

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
                if (MathHelper.CloseToPercent(_breakpoint, value)) return;
                _breakpoint = value;
                _breakpointSquared = _breakpoint * _breakpoint;
                Dirty();
            }
        }

        [DataField("breakpoint")]
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

        protected Joint() {}

        protected Joint(EntityUid bodyAUid, EntityUid bodyBUid)
        {
            BodyAUid = bodyAUid;
            BodyBUid = bodyBUid;

            //Can't connect a joint to the same body twice.
            Debug.Assert(BodyAUid != BodyBUid);
        }

        /// <summary>
        /// Applies our properties to the provided state
        /// </summary>
        /// <param name="state"></param>
        protected void GetState(JointState state)
        {
            state.ID = ID;
            state.CollideConnected = _collideConnected;
            state.Enabled = _enabled;
            state.UidA = BodyAUid;
            state.UidB = BodyBUid;
            state.Breakpoint = _breakpoint;
        }

        public abstract JointState GetState();

        internal virtual void ApplyState(JointState state)
        {
            ID = state.ID;
            CollideConnected = state.CollideConnected;
            Enabled = state.Enabled;
            Breakpoint = state.Breakpoint;
            _breakpointSquared = Breakpoint * Breakpoint;
            _localAnchorA = state.LocalAnchorA;
            _localAnchorB = state.LocalAnchorB;
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

        internal abstract void InitVelocityConstraints(SolverData data);

        internal float Validate(float invDt)
        {
            if (!Enabled)
                return 0.0f;

            var jointErrorSquared = GetReactionForce(invDt).LengthSquared;

            if (MathF.Abs(jointErrorSquared) <= _breakpointSquared)
                return 0.0f;

            Logger.DebugS("physics", $"Broke joint {ID}; force was {MathF.Sqrt(jointErrorSquared)}");
            Enabled = false;
            return jointErrorSquared;
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
                   MathHelper.CloseTo(_breakpoint, other._breakpoint) &&
                   _localAnchorA.EqualsApprox(other._localAnchorA) &&
                   _localAnchorB.EqualsApprox(other._localAnchorB);
        }

        // TODO: Need to check localanchor or something as well.
        public override int GetHashCode()
        {
            var hashcode = BodyAUid.GetHashCode();
            hashcode = hashcode * 397 ^ BodyBUid.GetHashCode();
            hashcode = hashcode * 397 ^ JointType.GetHashCode();
            return hashcode;
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
