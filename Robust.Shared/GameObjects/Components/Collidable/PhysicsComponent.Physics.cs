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
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    [ComponentReference(typeof(IPhysBody))]
    [NetworkedComponent(), ComponentProtoName("Physics")]
    public sealed class PhysicsComponent : Component, IPhysBody, ISerializationHooks, ILookupWorldBox2Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("status", readOnly: true)]
        private BodyStatus _bodyStatus = BodyStatus.OnGround;

        /// <summary>
        ///     Has this body been added to an island previously in this tick.
        /// </summary>
        public bool Island { get; set; }

        internal BroadphaseComponent? Broadphase { get; set; }

        /// <summary>
        ///     Store the body's index within the island so we can lookup its data.
        ///     Key is Island's ID and value is our index.
        /// </summary>
        public Dictionary<int, int> IslandIndex { get; set; } = new();

        // TODO: Actually implement after the initial pr dummy
        /// <summary>
        ///     Gets or sets where this body should be included in the CCD solver.
        /// </summary>
        public bool IsBullet { get; set; }

        public bool IgnoreCCD { get; set; }

        // TODO: Placeholder; look it's disgusting but my main concern is stopping fixtures being serialized every tick
        // on physics bodies for massive shuttle perf savings.
        [Obsolete("Use FixturesComponent instead.")]
        public IReadOnlyList<Fixture> Fixtures => _entMan.GetComponent<FixturesComponent>(Owner).Fixtures.Values.ToList();

        public int FixtureCount => _entMan.GetComponent<FixturesComponent>(Owner).Fixtures.Count;

        [ViewVariables] public int ContactCount => Contacts.Count;

        /// <summary>
        ///     Linked-list of all of our contacts.
        /// </summary>
        internal LinkedList<Contact> Contacts = new();

        public bool IgnorePaused { get; set; }

        internal SharedPhysicsMapComponent? PhysicsMap { get; set; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public BodyType BodyType
        {
            get => _bodyType;
            set
            {
                if (_bodyType == value)
                    return;

                var oldType = _bodyType;
                _bodyType = value;

                ResetMassData();

                if (_bodyType == BodyType.Static)
                {
                    SetAwake(false);
                    _linearVelocity = Vector2.Zero;
                    _angularVelocity = 0.0f;
                    // SynchronizeFixtures(); TODO: When CCD
                }
                else
                {
                    SetAwake(true);
                }

                Force = Vector2.Zero;
                Torque = 0.0f;

                EntitySystem.Get<SharedBroadphaseSystem>().RegenerateContacts(this);

                _entMan.EventBus.RaiseLocalEvent(Owner, new PhysicsBodyTypeChangedEvent(_bodyType, oldType), false);
            }
        }


        [DataField("bodyType")]
        private BodyType _bodyType = BodyType.Static;

        /// <summary>
        /// Set awake without the sleeptimer being reset.
        /// </summary>
        internal void ForceAwake()
        {
            if (_awake || _bodyType == BodyType.Static) return;

            _awake = true;
            _entMan.EventBus.RaiseEvent(EventSource.Local, new PhysicsWakeMessage(this));
        }

        // We'll also block Static bodies from ever being awake given they don't need to move.
        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Awake
        {
            get => _awake;
            set
            {
                if (_bodyType == BodyType.Static) return;

                SetAwake(value);
            }
        }

        internal bool _awake = true;

        private void SetAwake(bool value)
        {
            if (_awake == value) return;
            _awake = value;

            if (value)
            {
                _sleepTime = 0.0f;
                _entMan.EventBus.RaiseLocalEvent(Owner, new PhysicsWakeMessage(this));
            }
            else
            {
                _entMan.EventBus.RaiseLocalEvent(Owner, new PhysicsSleepMessage(this));
                ResetDynamics();
                _sleepTime = 0.0f;
            }

            Dirty(_entMan);
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
            set
            {
                if (_sleepingAllowed == value)
                    return;

                if (!value)
                    Awake = true;

                _sleepingAllowed = value;
                Dirty(_entMan);
            }
        }

        [DataField("sleepingAllowed")]
        private bool _sleepingAllowed = true;

        [ViewVariables]
        public float SleepTime
        {
            get => _sleepTime;
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (MathHelper.CloseToPercent(value, _sleepTime))
                    return;

                _sleepTime = value;
            }
        }

        [DataField("sleepTime")]
        private float _sleepTime;

        /// <inheritdoc />
        public void WakeBody()
        {
            Awake = true;
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_canCollide, _sleepingAllowed, _fixedRotation, _bodyStatus, _linearVelocity, _angularVelocity, _bodyType);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not PhysicsComponentState newState)
                return;

            SleepingAllowed = newState.SleepingAllowed;
            FixedRotation = newState.FixedRotation;
            CanCollide = newState.CanCollide;
            BodyStatus = newState.Status;

            // So transform doesn't apply MapId in the HandleComponentState because ??? so MapId can still be 0.
            // Fucking kill me, please. You have no idea deep the rabbit hole of shitcode goes to make this work.

            Dirty(_entMan);
            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            BodyType = newState.BodyType;
            Predict = false;
        }

        /// <summary>
        /// Resets the dynamics of this body.
        /// Sets torque, force and linear/angular velocity to 0
        /// </summary>
        public void ResetDynamics()
        {
            Torque = 0;
            _angularVelocity = 0;
            Force = Vector2.Zero;
            _linearVelocity = Vector2.Zero;
            Dirty(_entMan);
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null)
        {
            worldPos ??= _entMan.GetComponent<TransformComponent>(Owner).WorldPosition;
            worldRot ??= _entMan.GetComponent<TransformComponent>(Owner).WorldRotation;
            var transform = new Transform(worldPos.Value, (float) worldRot.Value.Theta);

            var bounds = new Box2(transform.Position, transform.Position);

            foreach (var fixture in _entMan.GetComponent<FixturesComponent>(Owner).Fixtures.Values)
            {
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var boundy = fixture.Shape.ComputeAABB(transform, i);
                    bounds = bounds.Union(boundy);
                }
            }

            return bounds;
        }

        public Box2 GetWorldAABB(Vector2 worldPos, Angle worldRot, EntityQuery<FixturesComponent> fixtures)
        {
            var transform = new Transform(worldPos, (float) worldRot.Theta);

            var bounds = new Box2(transform.Position, transform.Position);

            foreach (var fixture in fixtures.GetComponent(Owner).Fixtures.Values)
            {
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var boundy = fixture.Shape.ComputeAABB(transform, i);
                    bounds = bounds.Union(boundy);
                }
            }

            return bounds;
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
            set
            {
                if (_canCollide == value ||
                    value && Owner.IsInContainer())
                    return;

                _canCollide = value;
                _entMan.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(this, Owner, _canCollide));
                Dirty(_entMan);
            }
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

        // I made Mass read-only just because overwriting it doesn't touch inertia.
        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Mass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _mass : 0.0f;

        private float _mass;

        /// <summary>
        ///     Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        [ViewVariables]
        public float InvMass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _invMass : 0.0f;

        private float _invMass;

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
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (_bodyType != BodyType.Dynamic) return;

                if (MathHelper.CloseToPercent(_inertia, value)) return;

                if (value > 0.0f && !_fixedRotation)
                {
                    _inertia = value - Mass * Vector2.Dot(_localCenter, _localCenter);
                    DebugTools.Assert(_inertia > 0.0f);
                    InvI = 1.0f / _inertia;
                    Dirty(_entMan);
                }
            }
        }

        private float _inertia;

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
            set
            {
                if (_fixedRotation == value)
                    return;

                _fixedRotation = value;
                _angularVelocity = 0.0f;
                ResetMassData();
                Dirty(_entMan);
            }
        }

        // TODO: Should default to false someday IMO
        [DataField("fixedRotation")]
        private bool _fixedRotation = true;

        /// <summary>
        ///     Get this body's center of mass offset to world position.
        /// </summary>
        /// <remarks>
        ///     AKA Sweep.LocalCenter in Box2D.
        ///     Not currently in use as this is set after mass data gets set (when fixtures update).
        /// </remarks>
        public Vector2 LocalCenter
        {
            get => _localCenter;
            set
            {
                if (_bodyType != BodyType.Dynamic) return;

                if (value.EqualsApprox(_localCenter)) return;

                _localCenter = value;
            }
        }

        private Vector2 _localCenter = Vector2.Zero;

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
            set
            {
                if (MathHelper.CloseToPercent(value, _friction))
                    return;

                _friction = value;
                // TODO
                // Dirty(_entMan);
            }
        }

        private float _friction;

        /// <summary>
        ///     This is a set amount that the body's linear velocity is reduced by every tick.
        ///     Combined with the tile friction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float LinearDamping
        {
            get => _linearDamping;
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (MathHelper.CloseToPercent(value, _linearDamping))
                    return;

                _linearDamping = value;
                // Dirty(_entMan);
            }
        }

        [DataField("linearDamping")]
        private float _linearDamping = 0.2f;

        /// <summary>
        ///     This is a set amount that the body's angular velocity is reduced every tick.
        ///     Combined with the tile friction.
        /// </summary>
        /// <returns></returns>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularDamping
        {
            get => _angularDamping;
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (MathHelper.CloseToPercent(value, _angularDamping))
                    return;

                _angularDamping = value;
                // Dirty(_entMan);
            }
        }

        [DataField("angularDamping")]
        private float _angularDamping = 0.2f;

        /// <summary>
        /// Get the linear and angular velocities at the same time.
        /// </summary>
        public (Vector2 Linear, float Angular) MapVelocities
        {
            get
            {
                var linearVelocity = _linearVelocity;
                var angularVelocity = _angularVelocity;
                var entMan = _entMan;
                var parent = entMan.GetComponent<TransformComponent>(Owner).Parent;

                while (parent != null)
                {
                    if (entMan.TryGetComponent(parent.Owner, out PhysicsComponent? body))
                    {
                        linearVelocity += body.LinearVelocity;
                        angularVelocity += body.AngularVelocity;
                    }

                    parent = parent.Parent;
                }

                return (linearVelocity, angularVelocity);
            }
        }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LinearVelocity
        {
            get => _linearVelocity;
            set
            {
                // Curse you Q
                // DebugTools.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y));

                if (BodyType == BodyType.Static)
                    return;

                if (Vector2.Dot(value, value) > 0.0f)
                    Awake = true;

                if (_linearVelocity.EqualsApprox(value, 0.0001f))
                    return;

                _linearVelocity = value;
                Dirty(_entMan);
            }
        }

        internal Vector2 _linearVelocity;

        /// <summary>
        /// Get the body's LinearVelocity in map terms.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="MapVelocities"/> if you need linear and angular at the same time.
        /// </remarks>
        [ViewVariables]
        public Vector2 MapLinearVelocity
        {
            get
            {
                var entManager = IoCManager.Resolve<IEntityManager>();
                var physicsSystem = EntitySystem.Get<SharedPhysicsSystem>();
                var xforms = entManager.GetEntityQuery<TransformComponent>();
                var physics = entManager.GetEntityQuery<PhysicsComponent>();
                var xform = xforms.GetComponent(Owner);
                var parent = xform.ParentUid;
                var localPos = xform.LocalPosition;

                var velocity = _linearVelocity;

                while (parent.IsValid())
                {
                    var parentXform = xforms.GetComponent(parent);

                    if (physics.TryGetComponent(parent, out var body))
                    {
                        velocity += physicsSystem.GetLinearVelocityFromLocalPoint(body, localPos);
                    }

                    velocity = parentXform.LocalRotation.RotateVec(velocity);
                    parent = parentXform.ParentUid;
                }

                return velocity;
            }
        }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularVelocity
        {
            get => _angularVelocity;
            set
            {
                // TODO: This and linearvelocity asserts
                // DebugTools.Assert(!float.IsNaN(value));

                if (BodyType == BodyType.Static)
                    return;

                if (value * value > 0.0f)
                    Awake = true;

                if (MathHelper.CloseToPercent(_angularVelocity, value, 0.0001f))
                    return;

                _angularVelocity = value;
                Dirty(_entMan);
            }
        }

        private float _angularVelocity;

        /// <summary>
        /// Get the body's AngularVelocity in map terms.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="MapVelocities"/> if you need linear and angular at the same time.
        /// </remarks>
        [ViewVariables]
        public float MapAngularVelocity
        {
            get
            {
                var velocity = _angularVelocity;
                var entMan = _entMan;
                var parent = entMan.GetComponent<TransformComponent>(Owner).Parent;

                while (parent != null)
                {
                    if (entMan.TryGetComponent(parent.Owner, out PhysicsComponent? body))
                    {
                        velocity += body.AngularVelocity;
                    }

                    parent = parent.Parent;
                }

                return velocity;
            }
        }

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Momentum
        {
            get => LinearVelocity * Mass;
            set => LinearVelocity = value / Mass;
        }

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

        public IEnumerable<PhysicsComponent> GetBodiesIntersecting()
        {
            foreach (var entity in EntitySystem.Get<SharedPhysicsSystem>().GetCollidingEntities(_entMan.GetComponent<TransformComponent>(Owner).MapID, GetWorldAABB()))
            {
                yield return entity;
            }
        }

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
            var (worldPos, worldRot) = _entMan.GetComponent<TransformComponent>(Owner).GetWorldPositionRotation();

            var xf = new Transform(worldPos, (float) worldRot.Theta);
            // xf.Position -= Transform.Mul(xf.Quaternion2D, LocalCenter);

            return xf;
        }

        /// <summary>
        /// Applies an impulse to the centre of mass.
        /// </summary>
        public void ApplyLinearImpulse(in Vector2 impulse)
        {
            if ((_bodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0) return;
            Awake = true;

            LinearVelocity += impulse * _invMass;
        }

        /// <summary>
        /// Applies an impulse from the specified point.
        /// </summary>
        public void ApplyLinearImpulse(in Vector2 impulse, in Vector2 point)
        {
            if ((_bodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0) return;
            Awake = true;

            LinearVelocity += impulse * _invMass;
            // TODO: Sweep here
            AngularVelocity += InvI * Vector2.Cross(point, impulse);
        }

        public void ApplyAngularImpulse(float impulse)
        {
            if ((_bodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0) return;
            Awake = true;

            AngularVelocity += impulse * InvI;
        }

        public void ApplyForce(in Vector2 force)
        {
            if (_bodyType != BodyType.Dynamic) return;

            Awake = true;
            Force += force;
        }

        // TOOD: Need SetTransformIgnoreContacts so we can teleport body and /ignore contacts/
        public void DestroyContacts()
        {
            var node = Contacts.First;

            while (node != null)
            {
                var contact = node.Value;
                node = node.Next;
                PhysicsMap?.ContactManager.Destroy(contact);
            }

            DebugTools.Assert(Contacts.Count == 0);
        }

        IEnumerable<IPhysBody> IPhysBody.GetCollidingEntities(Vector2 offset, bool approx)
        {
            return EntitySystem.Get<SharedPhysicsSystem>().GetCollidingEntities(this, offset, approx);
        }

        public void ResetMassData(FixturesComponent? fixtures = null)
        {
            _mass = 0.0f;
            _invMass = 0.0f;
            _inertia = 0.0f;
            InvI = 0.0f;
            _localCenter = Vector2.Zero;

            if (((int) _bodyType & (int) BodyType.Kinematic) != 0)
            {
                return;
            }

            // Temporary until ECS don't @ me.
            fixtures ??= IoCManager.Resolve<IEntityManager>().GetComponent<FixturesComponent>(Owner);
            var localCenter = Vector2.Zero;
            var shapeManager = EntitySystem.Get<FixtureSystem>();

            foreach (var (_, fixture) in fixtures.Fixtures)
            {
                if (fixture.Mass <= 0.0f) continue;

                var data = new MassData {Mass = fixture.Mass};
                shapeManager.GetMassData(fixture.Shape, ref data);

                _mass += data.Mass;
                localCenter += data.Center * data.Mass;
                _inertia += data.I;
            }

            if (BodyType == BodyType.Static)
            {
                return;
            }

            if (_mass > 0.0f)
            {
                _invMass = 1.0f / _mass;
                localCenter *= _invMass;
            }
            else
            {
                // Always need positive mass.
                _mass = 1.0f;
                _invMass = 1.0f;
            }

            if (_inertia > 0.0f && !_fixedRotation)
            {
                // Center inertia about center of mass.
                _inertia -= _mass * Vector2.Dot(localCenter, localCenter);

                DebugTools.Assert(_inertia > 0.0f);
                InvI = 1.0f / _inertia;
            }
            else
            {
                _inertia = 0.0f;
                InvI = 0.0f;
            }

            _localCenter = localCenter;

            // TODO: Calculate Sweep

            /*
            var oldCenter = Sweep.Center;
            Sweep.LocalCenter = localCenter;
            Sweep.Center0 = Sweep.Center = Transform.Mul(GetTransform(), Sweep.LocalCenter);
            */

            // Update center of mass velocity.
            // _linVelocity += Vector2.Cross(_angVelocity, Worl - oldCenter);

        }

        /// <summary>
        ///     Used to prevent bodies from colliding; may lie depending on joints.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal bool ShouldCollide(PhysicsComponent other)
        {
            if ((_bodyType & (BodyType.Kinematic | BodyType.Static)) != 0 &&
                (other._bodyType & (BodyType.Kinematic | BodyType.Static)) != 0)
            {
                return false;
            }

            // Does a joint prevent collision?
            // if one of them doesn't have jointcomp then they can't share a common joint.
            // otherwise, only need to iterate over the joints of one component as they both store the same joint.
            if (_entMan.TryGetComponent(Owner, out JointComponent? jointComponentA) &&
                _entMan.TryGetComponent(other.Owner, out JointComponent? jointComponentB))
            {
                var aUid = jointComponentA.Owner;
                var bUid = jointComponentB.Owner;

                foreach (var (_, joint) in jointComponentA.Joints)
                {
                    // Check if either: the joint even allows collisions OR the other body on the joint is actually the other body we're checking.
                    if (!joint.CollideConnected &&
                        (aUid == joint.BodyAUid &&
                         bUid == joint.BodyBUid) ||
                        (bUid == joint.BodyAUid ||
                         aUid == joint.BodyBUid)) return false;
                }
            }

            var preventCollideMessage = new PreventCollideEvent(this, other);
            _entMan.EventBus.RaiseLocalEvent(Owner, preventCollideMessage);

            if (preventCollideMessage.Cancelled) return false;

            preventCollideMessage = new PreventCollideEvent(other, this);
            _entMan.EventBus.RaiseLocalEvent(other.Owner, preventCollideMessage);

            if (preventCollideMessage.Cancelled) return false;

            return true;
        }
    }

    /// <summary>
    ///     Directed event raised when an entity's physics BodyType changes.
    /// </summary>
    public class PhysicsBodyTypeChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     New BodyType of the entity.
        /// </summary>
        public BodyType New { get; }

        /// <summary>
        ///     Old BodyType of the entity.
        /// </summary>
        public BodyType Old { get; }

        public PhysicsBodyTypeChangedEvent(BodyType newType, BodyType oldType)
        {
            New = newType;
            Old = oldType;
        }
    }
}
