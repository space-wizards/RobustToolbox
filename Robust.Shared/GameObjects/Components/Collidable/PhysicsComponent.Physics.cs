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
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    [ComponentReference(typeof(IPhysBody))]
    [NetworkedComponent(), ComponentProtoName("Physics")]
    public sealed class PhysicsComponent : Component, IPhysBody, ILookupWorldBox2Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        [DataField("status", readOnly: true)]
        private BodyStatus _bodyStatus = BodyStatus.OnGround;

        /// <summary>
        ///     Has this body been added to an island previously in this tick.
        /// </summary>
        public bool Island { get; set; }

        [ViewVariables]
        internal BroadphaseComponent? Broadphase { get; set; }

        /// <summary>
        /// Debugging VV
        /// </summary>
        [ViewVariables]
        private Box2? _broadphaseAABB
        {
            get
            {
                Box2? aabb = null;

                if (Broadphase == null)
                {
                    return aabb;
                }

                var tree = Broadphase.Tree;

                foreach (var (_, fixture) in IoCManager.Resolve<IEntityManager>().GetComponent<FixturesComponent>(Owner).Fixtures)
                {
                    foreach (var proxy in fixture.Proxies)
                    {
                        aabb = aabb?.Union(tree.GetProxy(proxy.ProxyId)!.AABB) ?? tree.GetProxy(proxy.ProxyId)!.AABB;
                    }
                }

                return aabb;
            }
        }

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

        [DataField("ignorePaused"), ViewVariables(VVAccess.ReadWrite)]
        public bool IgnorePaused { get; set; }

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
                // Even if it's dynamic if it can't collide then don't force it awake.
                else if (_canCollide)
                {
                    SetAwake(true);
                }

                Force = Vector2.Zero;
                Torque = 0.0f;

                _sysMan.GetEntitySystem<SharedBroadphaseSystem>().RegenerateContacts(this);

                var ev = new PhysicsBodyTypeChangedEvent(Owner, _bodyType, oldType, this);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
            }
        }


        [DataField("bodyType")]
        private BodyType _bodyType = BodyType.Static;

        // We'll also block Static bodies from ever being awake given they don't need to move.
        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Awake
        {
            get => _awake;
            set => SetAwake(value);
        }

        private bool _awake = false;

        public void SetAwake(bool value, bool updateSleepTime = true)
        {
            if (_awake == value)
                return;

            if (value && _bodyType == BodyType.Static)
                return;

            // TODO: Remove this. Need to think of just making Awake read-only and just having WakeBody / SleepBody
            if (value && !_canCollide)
            {
                CanCollide = true;
                if (!_canCollide)
                    return;
            }

            _awake = value;

            if (value)
            {
                var ev = new PhysicsWakeEvent(this);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
            }
            else
            {
                var ev = new PhysicsSleepEvent(this);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
                ResetDynamics();
            }

            if (updateSleepTime)
                _sleepTime = 0.0f;

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
            CanCollide = true;

            if (!_canCollide) return;

            Awake = true;
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
            set
            {
                if (_canCollide == value)
                    return;

                // If we're recursively in a container then never set this.
                if (value && _entMan.EntitySysManager.GetEntitySystem<SharedContainerSystem>()
                    .IsEntityOrParentInContainer(Owner)) return;

                if (!value)
                {
                    Awake = false;
                }

                _canCollide = value;
                var ev = new CollisionChangeEvent(this, _canCollide);
                _entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
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
        [ViewVariables]
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

                // CloseToPercent tolerance needs to be small enough such that an angular velocity just above
                // sleep-tolerance can damp down to sleeping.

                if (MathHelper.CloseToPercent(_angularVelocity, value, 0.00001f))
                    return;

                _angularVelocity = value;
                Dirty(_entMan);
            }
        }

        internal float _angularVelocity;

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
            foreach (var entity in _sysMan.GetEntitySystem<SharedPhysicsSystem>().GetCollidingEntities(_entMan.GetComponent<TransformComponent>(Owner).MapID, GetWorldAABB()))
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
            return GetTransform(_entMan.GetComponent<TransformComponent>(Owner));
        }

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

        public void ResetMassData(FixturesComponent? fixtures = null)
        {
            _mass = 0.0f;
            _invMass = 0.0f;
            _inertia = 0.0f;
            InvI = 0.0f;
            _localCenter = Vector2.Zero;

            // Temporary until ECS don't @ me.
            fixtures ??= IoCManager.Resolve<IEntityManager>().GetComponent<FixturesComponent>(Owner);
            var localCenter = Vector2.Zero;

            foreach (var (_, fixture) in fixtures.Fixtures)
            {
                if (fixture.Mass <= 0.0f) continue;

                var data = new MassData {Mass = fixture.Mass};
                FixtureSystem.GetMassData(fixture.Shape, ref data);

                _mass += data.Mass;
                localCenter += data.Center * data.Mass;
                _inertia += data.I;
            }

            // Update this after re-calculating mass as content may want to use the sum of fixture masses instead.
            if (((int) _bodyType & (int) (BodyType.Kinematic | BodyType.Static)) != 0)
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
                        (bUid == joint.BodyAUid &&
                         aUid == joint.BodyBUid)) return false;
                }
            }

            var preventCollideMessage = new PreventCollideEvent(this, other);
            _entMan.EventBus.RaiseLocalEvent(Owner, preventCollideMessage, true);

            if (preventCollideMessage.Cancelled) return false;

            preventCollideMessage = new PreventCollideEvent(other, this);
            _entMan.EventBus.RaiseLocalEvent(other.Owner, preventCollideMessage, true);

            if (preventCollideMessage.Cancelled) return false;

            return true;
        }

        // View variables conveniences properties.
        [ViewVariables]
        private Vector2 _mapLinearVelocity => _sysMan.GetEntitySystem<SharedPhysicsSystem>().GetMapLinearVelocity(Owner, this);
        [ViewVariables]
        private float _mapAngularVelocity => _sysMan.GetEntitySystem<SharedPhysicsSystem>().GetMapAngularVelocity(Owner, this);
    }

    /// <summary>
    ///     Directed event raised when an entity's physics BodyType changes.
    /// </summary>
    [ByRefEvent]
    public readonly struct PhysicsBodyTypeChangedEvent
    {
        public readonly EntityUid Entity;

        /// <summary>
        ///     New BodyType of the entity.
        /// </summary>
        public readonly BodyType New;

        /// <summary>
        ///     Old BodyType of the entity.
        /// </summary>
        public readonly BodyType Old;

        public readonly PhysicsComponent Component;

        public PhysicsBodyTypeChangedEvent(EntityUid entity, BodyType newType, BodyType oldType, PhysicsComponent component)
        {
            Entity = entity;
            New = newType;
            Old = oldType;
            Component = component;
        }
    }
}
