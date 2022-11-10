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
 *
 * PhysicsComponent is heavily modified from Box2D.
*/

using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Components
{
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    [ComponentReference(typeof(IPhysBody))]
    [NetworkedComponent(), ComponentProtoName("Physics")]
    public sealed class PhysicsComponent : Component, IPhysBody, ILookupWorldBox2Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("status", readOnly: true)]
        private BodyStatus _bodyStatus = BodyStatus.OnGround;

        /// <summary>
        ///     Has this body been added to an island previously in this tick.
        /// </summary>
        public bool Island { get; set; }

        /// <summary>
        ///     Store the body's index within the island so we can lookup its data.
        ///     Key is Island's ID and value is our index.
        /// </summary>
        public Dictionary<int, int> IslandIndex { get; set; } = new();

        [Obsolete("use FixtureSystem.FixtureCount")]
        public int FixtureCount => _entMan.GetComponent<FixturesComponent>(Owner).Fixtures.Count;

        [ViewVariables] public int ContactCount => Contacts.Count;

        /// <summary>
        ///     Linked-list of all of our contacts.
        /// </summary>
        internal readonly LinkedList<Contact> Contacts = new();

        [DataField("ignorePaused"), ViewVariables(VVAccess.ReadWrite)]
        public bool IgnorePaused { get; set; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public BodyType BodyType
        {
            get => _bodyType;
            [Obsolete("Use SharedPhysicsSystem.SetBodyType")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetBodyType(this, value);
        }

        [DataField("bodyType")] internal BodyType _bodyType = BodyType.Static;

        // We'll also block Static bodies from ever being awake given they don't need to move.
        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Awake
        {
            get => _awake;
            [Obsolete("Use SharedPhysicsSystem.SetAwake")]
            set => SetAwake(value);
        }

        internal bool _awake = false;

        [Obsolete("Use SharedPhysicsSystem.SetAwake")]
        public void SetAwake(bool value, bool updateSleepTime = true)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetAwake(this, value, updateSleepTime);
        }

        /// <summary>
        /// You can disable sleeping on this body. If you disable sleeping, the
        /// body will be woken.
        /// </summary>
        /// <value><c>true</c> if sleeping is allowed; otherwise, <c>false</c>.</value>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SleepingAllowed
        {
            get => _sleepingAllowed;
            [Obsolete("Use SharedPhysicsSystem.SetSleepingAllowed")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetSleepingAllowed(this, value);
        }

        [DataField("sleepingAllowed")] internal bool _sleepingAllowed = true;

        [ViewVariables]
        public float SleepTime
        {
            get => _sleepTime;
            [Obsolete("Use SharedPhysicsSystem.SetSleepTime")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetSleepTime(this, value);
        }

        [DataField("sleepTime")] internal float _sleepTime;

        /// <inheritdoc />
        [Obsolete("Use SharedPhysicsSystem.WakeBody")]
        public void WakeBody()
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().WakeBody(this);
        }

        /// <summary>
        /// Resets the dynamics of this body.
        /// Sets torque, force and linear/angular velocity to 0
        /// </summary>
        [Obsolete("Use SharedPhysicsSystem.ResetDynamics")]
        public void ResetDynamics()
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ResetDynamics(this);
        }

        public Box2 GetAABB(Transform transform)
        {
            var bounds = new Box2(transform.Position, transform.Position);

            // Applying transform component state can cause entity-lookup updates, which apparently sometimes trigger this
            // function before a fixtures has been added? I'm not 100% sure how this happens.
            if (!_entMan.TryGetComponent(Owner, out FixturesComponent? fixtures))
                return bounds;

            // TODO cache this to speed up entity lookups & tree updating
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    // TODO don't transform each fixture, just transform the final AABB
                    var boundy = fixture.Shape.ComputeAABB(transform, i);
                    bounds = bounds.Union(boundy);
                }
            }

            return bounds;
        }

        [Obsolete("Use the GetWorldAABB on EntityLookupSystem")]
        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null)
        {
            if (worldPos == null && worldRot == null)
            {
                (worldPos, worldRot) = _entMan.GetComponent<TransformComponent>(Owner).GetWorldPositionRotation();
            }
            else
            {
                worldPos ??= _entMan.GetComponent<TransformComponent>(Owner).WorldPosition;
                worldRot ??= _entMan.GetComponent<TransformComponent>(Owner).WorldRotation;
            }

            return GetAABB(new Transform(worldPos.Value, (float)worldRot.Value.Theta));
        }

        /// <summary>
        ///     Enables or disabled collision processing of this component.
        /// </summary>
        /// <remarks>
        ///     Also known as Enabled in Box2D
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanCollide
        {
            get => _canCollide;
            [Obsolete("Use SharedPhysicsSystem.SetCanCollide")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetCanCollide(this, value);
        }

        [DataField("canCollide")] internal bool _canCollide = true;

        /// <summary>
        ///     Non-hard physics bodies will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events. Recommended you use the fixture hard values directly
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Hard { get; internal set; }

        /// <summary>
        ///     Bitmask of the collision layers this component is a part of.
        /// </summary>
        [ViewVariables]
        public int CollisionLayer { get; internal set; }

        /// <summary>
        ///     Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables]
        public int CollisionMask { get; internal set; }

        /// <summary>
        ///     The current total mass of the entities fixtures in kilograms. Ignores the body type.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float FixturesMass => _mass;

        // I made Mass read-only just because overwriting it doesn't touch inertia.
        /// <summary>
        ///     Current mass of the entity in kilograms. This may be 0 depending on the body type.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float Mass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _mass : 0.0f;

        internal float _mass;

        /// <summary>
        ///     Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        [ViewVariables]
        public float InvMass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _invMass : 0.0f;

        internal float _invMass;

        /// <summary>
        /// Moment of inertia, or angular mass, in kg * m^2.
        /// </summary>
        /// <remarks>
        /// https://en.wikipedia.org/wiki/Moment_of_inertia
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Inertia
        {
            get => _inertia + _mass * Vector2.Dot(_localCenter, _localCenter);
            [Obsolete("Use SharedPhysicsSystem.Inertia")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetInertia(this, value);
        }

        internal float _inertia;

        /// <summary>
        ///     Indicates whether this body ignores gravity
        /// </summary>
        public bool IgnoreGravity { get; set; }

        /// <summary>
        /// Inverse moment of inertia (1 / I).
        /// </summary>
        [ViewVariables]
        public float InvI { get; set; }

        /// <summary>
        ///     Is the body allowed to have angular velocity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool FixedRotation
        {
            get => _fixedRotation;
            [Obsolete("Use SharedPhysicsSystem.SetFixedRotation")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetFixedRotation(this, value);
        }

        // TODO: Should default to false someday IMO
        [DataField("fixedRotation")] internal bool _fixedRotation = true;

        /// <summary>
        ///     Get this body's center of mass offset to world position.
        /// </summary>
        [ViewVariables]
        public Vector2 LocalCenter
        {
            get => _localCenter;
            [Obsolete("Use SharedPhysicsSystem.SetLocalCenter")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetLocalCenter(this, value);
        }

        internal Vector2 _localCenter = Vector2.Zero;

        /// <summary>
        /// Current Force being applied to this entity in Newtons.
        /// </summary>
        /// <remarks>
        /// The force is applied to the center of mass.
        /// https://en.wikipedia.org/wiki/Force
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Force { get; set; }

        /// <summary>
        /// Current torque being applied to this entity in N*m.
        /// </summary>
        /// <remarks>
        /// The torque rotates around the Z axis on the object.
        /// https://en.wikipedia.org/wiki/Torque
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Torque { get; set; }

        /// <summary>
        ///     Contact friction between 2 bodies.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Friction
        {
            get => _friction;
            [Obsolete("Use SharedPhysicsSystem.SetFriction")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetFriction(this, value);
        }

        internal float _friction;

        /// <summary>
        ///     This is a set amount that the body's linear velocity is reduced by every tick.
        ///     Combined with the tile friction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float LinearDamping
        {
            get => _linearDamping;
            [Obsolete("Use SharedPhysicsSystem.SetLinearDamping")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetLinearDamping(this, value);
        }

        [DataField("linearDamping")] internal float _linearDamping = 0.2f;

        /// <summary>
        ///     This is a set amount that the body's angular velocity is reduced every tick.
        ///     Combined with the tile friction.
        /// </summary>
        /// <returns></returns>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularDamping
        {
            get => _angularDamping;
            [Obsolete("Use SharedPhysicsSystem.SetAngularDamping")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetAngularDamping(this, value);
        }

        [DataField("angularDamping")] internal float _angularDamping = 0.2f;

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        /// <remarks>
        ///     This is the velocity relative to the parent, but is defined in terms of map coordinates. I.e., if the
        ///     entity's parents are all stationary, this is the rate of change of this entity's world position (not
        ///     local position).
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LinearVelocity
        {
            get => _linearVelocity;
            [Obsolete("Use SharedPhysicsSystem.SetLinearVelocity")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetLinearVelocity(this, value);
        }

        internal Vector2 _linearVelocity;

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularVelocity
        {
            get => _angularVelocity;
            [Obsolete("Use SharedPhysicsSystem.SetAngularVelocity")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetAngularVelocity(this, value);
        }

        internal float _angularVelocity;

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Momentum => LinearVelocity * Mass;

        /// <summary>
        ///     The current status of the object
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public BodyStatus BodyStatus
        {
            get => _bodyStatus;
            set
            {
                if (_bodyStatus == value)
                    return;

                _bodyStatus = value;
                Dirty(_entMan);
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Predict
        {
            get => _predict;
            set => _predict = value;
        }

        private bool _predict;

        /// <summary>
        /// Gets a local point relative to the body's origin given a world point.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="worldPoint">A point in world coordinates.</param>
        /// <returns>The corresponding local point relative to the body's origin.</returns>
        public Vector2 GetLocalPoint(in Vector2 worldPoint)
        {
            return Transform.MulT(GetTransform(), worldPoint);
        }

        /// <summary>
        /// Get the world coordinates of a point given the local coordinates.
        /// </summary>
        /// <param name="localPoint">A point on the body measured relative the the body's origin.</param>
        /// <returns>The same point expressed in world coordinates.</returns>
        public Vector2 GetWorldPoint(in Vector2 localPoint)
        {
            return Transform.Mul(GetTransform(), localPoint);
        }

        public Vector2 GetLocalVector2(Vector2 worldVector)
        {
            return Transform.MulT(new Quaternion2D((float) _entMan.GetComponent<TransformComponent>(Owner).WorldRotation.Theta), worldVector);
        }

        public Transform GetTransform()
        {
            return GetTransform(_entMan.GetComponent<TransformComponent>(Owner));
        }

        [Obsolete("Use SharedPhysicsSystem.GetPhysicsTransform")]
        public Transform GetTransform(TransformComponent xform)
        {
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            var xf = new Transform(worldPos, (float) worldRot.Theta);
            // xf.Position -= Transform.Mul(xf.Quaternion2D, LocalCenter);

            return xf;
        }

        /// <summary>
        /// Applies an impulse to the centre of mass.
        /// </summary>
        [Obsolete("Use SharedPhysicsSystem.ApplyLinearImpulse")]
        public void ApplyLinearImpulse(in Vector2 impulse)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ApplyLinearImpulse(this, impulse);
        }

        /// <summary>
        /// Applies an impulse from the specified point.
        /// </summary>
        [Obsolete("Use SharedPhysicsSystem.ApplyLinearImpulse")]
        public void ApplyLinearImpulse(in Vector2 impulse, in Vector2 point)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ApplyLinearImpulse(this, impulse, point);
        }

        [Obsolete("Use SharedPhysicsSystem.ApplyAngularImpulse")]
        public void ApplyAngularImpulse(float impulse)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ApplyAngularImpulse(this, impulse);
        }

        [Obsolete("Use SharedPhysicsSystem.ApplyForce")]
        public void ApplyForce(in Vector2 force)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ApplyForce(this, force);
        }

        [Obsolete("Use SharedPhysicsSystem.ResetMassData")]
        public void ResetMassData(FixturesComponent? fixtures = null)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().ResetMassData(this, fixtures);
        }

        // View variables conveniences properties.
        [ViewVariables]
        private Vector2 _mapLinearVelocity => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().GetMapLinearVelocity(Owner, this);
        [ViewVariables]
        private float _mapAngularVelocity => _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().GetMapAngularVelocity(Owner, this);
    }
}
