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
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [ComponentReference(typeof(IPhysBody))]
    public sealed class PhysicsComponent : Component, IPhysBody, ISerializationHooks
    {
        [DataField("status", readOnly: true)]
        private BodyStatus _bodyStatus = BodyStatus.OnGround;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Has this body been added to an island previously in this tick.
        /// </summary>
        public bool Island { get; set; }

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

        /// <summary>
        ///     Linked-list of all of our contacts.
        /// </summary>
        internal ContactEdge? ContactEdges { get; set; } = null;

        /// <summary>
        ///     Linked-list of all of our joints.
        /// </summary>
        internal JointEdge? JointEdges { get; set; } = null;
        // TODO: Should there be a VV thing for joints? Would be useful. Same with contacts.
        // Though not sure how to do it well with the linked-list.

        public bool IgnorePaused { get; set; }

        internal PhysicsMap PhysicsMap { get; set; } = default!;

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
                    _linVelocity = Vector2.Zero;
                    _angVelocity = 0.0f;
                    // SynchronizeFixtures(); TODO: When CCD
                }
                else
                {
                    SetAwake(true);
                }

                Force = Vector2.Zero;
                Torque = 0.0f;

                RegenerateContacts();

                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PhysicsBodyTypeChangedEvent(_bodyType, oldType), false);
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
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsWakeMessage(this));
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

        private bool _awake = true;

        private void SetAwake(bool value)
        {
            if (_awake == value) return;
            _awake = value;

            if (value)
            {
                _sleepTime = 0.0f;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PhysicsWakeMessage(this));
            }
            else
            {
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PhysicsSleepMessage(this));
                ResetDynamics();
                _sleepTime = 0.0f;
            }

            Dirty();
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
                Dirty();
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

                if (MathHelper.CloseTo(value, _sleepTime))
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

        /// <summary>
        ///     Removes all of our contacts and flags them as requiring regeneration next physics tick.
        /// </summary>
        public void RegenerateContacts()
        {
            var contactEdge = ContactEdges;
            while (contactEdge != null)
            {
                var contactEdge0 = contactEdge;
                contactEdge = contactEdge.Next;
                PhysicsMap.ContactManager.Destroy(contactEdge0.Contact!);
            }

            ContactEdges = null;
            var broadphaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();

            foreach (var fixture in Fixtures)
            {
                var proxyCount = fixture.ProxyCount;
                foreach (var (gridId, proxies) in fixture.Proxies)
                {
                    var broadPhase = broadphaseSystem.GetBroadPhase(Owner.Transform.MapID, gridId);
                    if (broadPhase == null) continue;
                    for (var i = 0; i < proxyCount; i++)
                    {
                        broadPhase.TouchProxy(proxies[i].ProxyId);
                    }
                }
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var fixture in _fixtures)
            {
                fixture.Body = this;
                fixture.ComputeProperties();
                fixture.ID = GetFixtureName(fixture);
            }

            ResetMassData();

            if (_mass > 0f && (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0)
            {
                _invMass = 1.0f / _mass;
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState(ICommonSession session)
        {
            // TODO: Could optimise the shit out of this because only linear velocity and angular velocity are changing 99% of the time.
            var joints = new List<Joint>();
            for (var je = JointEdges; je != null; je = je.Next)
            {
                joints.Add(je.Joint);
            }

            return new PhysicsComponentState(_canCollide, _sleepingAllowed, _fixedRotation, _bodyStatus, _fixtures, joints, LinearVelocity, AngularVelocity, BodyType);
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

            // We will pray that this deferred joint is handled properly.

            /*
             * -- Joints --
             */

            // TODO: Iterating like this is inefficient and bloated as fuck but on the other hand the linked-list is very convenient
            // for bodies with a large number of fixtures / joints.
            // Probably store them in Dictionaries but still store the linked-list stuff on the fixture / joint itself.
            var existingJoints = Joints.ToList();
            var toAddJoints = new List<Joint>();
            var toRemoveJoints = new List<Joint>();

            foreach (var newJoint in newState.Joints)
            {
                var jointFound = false;

                foreach (var joint in existingJoints)
                {
                    if (joint.ID.Equals(newJoint.ID))
                    {
                        if (!newJoint.Equals(joint) && TrySetupNetworkedJoint(newJoint))
                        {
                            toAddJoints.Add(newJoint);
                            toRemoveJoints.Add(joint);
                        }

                        jointFound = true;
                        break;
                    }
                }

                if (!jointFound && TrySetupNetworkedJoint(newJoint))
                {
                    toAddJoints.Add(newJoint);
                }
            }

            foreach (var joint in existingJoints)
            {
                var jointFound = false;

                foreach (var newJoint in newState.Joints)
                {
                    if (joint.ID.Equals(newJoint.ID))
                    {
                        jointFound = true;
                        break;
                    }
                }

                if (jointFound) continue;

                toRemoveJoints.Add(joint);
            }

            foreach (var joint in toRemoveJoints)
            {
                RemoveJoint(joint);
            }

            foreach (var joint in toAddJoints)
            {
                AddJoint(joint);
            }

            /*
             * -- Fixtures --
             */

            var toAddFixtures = new List<Fixture>();
            var toRemoveFixtures = new List<Fixture>();
            var computeProperties = false;

            // Given a bunch of data isn't serialized need to sort of re-initialise it
            var newFixtures = new List<Fixture>(newState.Fixtures.Count);
            foreach (var fixture in newState.Fixtures)
            {
                var newFixture = new Fixture();
                fixture.CopyTo(newFixture);
                newFixture.Body = this;
                newFixtures.Add(newFixture);
            }

            // Add / update new fixtures
            foreach (var fixture in newFixtures)
            {
                var found = false;

                foreach (var existing in _fixtures)
                {
                    if (!fixture.ID.Equals(existing.ID)) continue;

                    if (!fixture.Equals(existing))
                    {
                        toAddFixtures.Add(fixture);
                        toRemoveFixtures.Add(existing);
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    toAddFixtures.Add(fixture);
                }
            }

            // Remove old fixtures
            foreach (var existing in _fixtures)
            {
                var found = false;

                foreach (var fixture in newFixtures)
                {
                    if (fixture.ID.Equals(existing.ID))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemoveFixtures.Add(existing);
                }
            }

            foreach (var fixture in toRemoveFixtures)
            {
                computeProperties = true;
                RemoveFixture(fixture);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            foreach (var fixture in toAddFixtures)
            {
                computeProperties = true;
                AddFixture(fixture);
                fixture.Shape.ApplyState();
            }

            /*
             * -- Sundries --
             */

            Dirty();
            if (computeProperties)
            {
                ResetMassData();
            }

            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            BodyType = newState.BodyType;
            Predict = false;
        }

        private bool TrySetupNetworkedJoint(Joint joint)
        {
            // This can fail if we've already deleted the entity or remove physics from it I think?

            if (!Owner.EntityManager.TryGetEntity(joint.BodyAUid, out var entityA) ||
                !entityA.TryGetComponent(out PhysicsComponent? bodyA))
            {
                return false;
            }

            if (!Owner.EntityManager.TryGetEntity(joint.BodyBUid, out var entityB) ||
                !entityB.TryGetComponent(out PhysicsComponent? bodyB))
            {
                return false;
            }

            joint.BodyA = bodyA;
            joint.BodyB = bodyB;
            joint.EdgeA = new JointEdge();
            joint.EdgeB = new JointEdge();
            return true;
        }

        public Fixture? GetFixture(string name)
        {
            // Sooo I'd rather have fixtures as a list in serialization but there's not really an easy way to have it as a
            // temporary value on deserialization so we can store it as a dictionary of <name, fixture>
            // given 100% of bodies have 1-2 fixtures this isn't really a performance problem right now but
            // should probably be done at some stage
            // If we really need it then you just deserialize onto a dummy field that then just never gets used again.
            foreach (var fixture in _fixtures)
            {
                if (fixture.ID.Equals(name))
                {
                    return fixture;
                }
            }

            return null;
        }

        /// <summary>
        /// Resets the dynamics of this body.
        /// Sets torque, force and linear/angular velocity to 0
        /// </summary>
        public void ResetDynamics()
        {
            Torque = 0;
            _angVelocity = 0;
            Force = Vector2.Zero;
            _linVelocity = Vector2.Zero;
            Dirty();
        }

        public Box2 GetWorldAABB(Vector2? worldPos = null, Angle? worldRot = null)
        {
            worldPos ??= Owner.Transform.WorldPosition;
            worldRot ??= Owner.Transform.WorldRotation;

            var worldPosValue = worldPos.Value;
            var worldRotValue = worldRot.Value;

            var bounds = new Box2(worldPosValue, worldPosValue);

            foreach (var fixture in _fixtures)
            {
                var boundy = fixture.Shape.CalculateLocalBounds(worldRotValue);
                bounds = bounds.Union(boundy.Translated(worldPosValue));
            }

            return bounds;
        }

        /// <inheritdoc />
        [ViewVariables]
        public IReadOnlyList<Fixture> Fixtures => _fixtures;

        public IEnumerable<Joint> Joints
        {
            get
            {
                JointEdge? edge = JointEdges;

                while (edge != null)
                {
                    yield return edge.Joint;
                    edge = edge.Next;
                }
            }
        }

        [DataField("fixtures")]
        [NeverPushInheritance]
        private List<Fixture> _fixtures = new();

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

                _canCollide = value;
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(this, Owner.Uid, _canCollide));
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsUpdateMessage(this));
                Dirty();
            }
        }

        [DataField("canCollide")]
        private bool _canCollide = true;

        /// <summary>
        ///     Non-hard physics bodies will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events. Recommended you use the fixture hard values directly
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Hard
        {
            get
            {
                foreach (var fixture in Fixtures)
                {
                    if (fixture.Hard) return true;
                }

                return false;
            }
            set
            {
                foreach (var fixture in Fixtures)
                {
                    fixture.Hard = value;
                }
            }
        }

        /// <summary>
        ///     Bitmask of the collision layers this component is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get
            {
                var layers = 0x0;

                foreach (var fixture in Fixtures)
                    layers |= fixture.CollisionLayer;
                return layers;
            }
        }

        /// <summary>
        ///     Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get
            {
                var mask = 0x0;

                foreach (var fixture in Fixtures)
                    mask |= fixture.CollisionMask;
                return mask;
            }
        }

        [ViewVariables]
        public bool HasProxies { get; set; }

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
            get => _inertia + Mass * Vector2.Dot(Vector2.Zero, Vector2.Zero); // TODO: Sweep.LocalCenter
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (_bodyType != BodyType.Dynamic) return;

                if (MathHelper.CloseTo(_inertia, value)) return;

                if (value > 0.0f && !_fixedRotation)
                {
                    _inertia = value - Mass * Vector2.Dot(LocalCenter, LocalCenter);
                    DebugTools.Assert(_inertia > 0.0f);
                    InvI = 1.0f / _inertia;
                    Dirty();
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
                _angVelocity = 0.0f;
                ResetMassData();
                Dirty();
            }
        }

        // TODO: Should default to false someday IMO
        [DataField("fixedRotation")]
        private bool _fixedRotation = true;

        // TODO: Will be used someday
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

                throw new NotImplementedException();
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
                if (MathHelper.CloseTo(value, _friction))
                    return;

                _friction = value;
                // TODO
                // Dirty();
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

                if (MathHelper.CloseTo(value, _linearDamping))
                    return;

                _linearDamping = value;
                // Dirty();
            }
        }

        [DataField("linearDamping")]
        private float _linearDamping = 0.02f;

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

                if (MathHelper.CloseTo(value, _angularDamping))
                    return;

                _angularDamping = value;
                // Dirty();
            }
        }

        [DataField("angularDamping")]
        private float _angularDamping = 0.02f;

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LinearVelocity
        {
            get => _linVelocity;
            set
            {
                // Curse you Q
                // DebugTools.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y));

                if (BodyType == BodyType.Static)
                    return;

                if (Vector2.Dot(value, value) > 0.0f)
                    Awake = true;

                if (_linVelocity.EqualsApprox(value, 0.0001))
                    return;

                _linVelocity = value;
                Dirty();
            }
        }

        private Vector2 _linVelocity;

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularVelocity
        {
            get => _angVelocity;
            set
            {
                // TODO: This and linearvelocity asserts
                // DebugTools.Assert(!float.IsNaN(value));

                if (BodyType == BodyType.Static)
                    return;

                if (value * value > 0.0f)
                    Awake = true;

                if (MathHelper.CloseTo(_angVelocity, value))
                    return;

                _angVelocity = value;
                Dirty();
            }
        }

        private float _angVelocity;

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
                Dirty();
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
        ///     As we defer updates need to store the MapId we used for broadphase.
        /// </summary>
        public MapId BroadphaseMapId { get; set; }

        public IEnumerable<PhysicsComponent> GetCollidingBodies()
        {
            foreach (var entity in EntitySystem.Get<SharedBroadPhaseSystem>().GetCollidingEntities(this, Vector2.Zero))
            {
                yield return entity;
            }
        }

        public IEnumerable<PhysicsComponent> GetBodiesIntersecting()
        {
            foreach (var entity in EntitySystem.Get<SharedBroadPhaseSystem>().GetCollidingEntities(Owner.Transform.MapID, GetWorldAABB()))
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
            return Physics.Transform.MulT(GetTransform(), worldPoint);
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

        /// <summary>
        ///     Remove the proxies from all the broadphases.
        /// </summary>
        public void ClearProxies()
        {
            if (!HasProxies) return;

            var broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
            var mapId = BroadphaseMapId;

            foreach (var fixture in Fixtures)
            {
                fixture.ClearProxies(mapId, broadPhaseSystem);
            }

            HasProxies = false;
        }

        public void FixtureChanged(Fixture fixture)
        {
            // TODO: Optimise this a LOT
            Dirty();
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new FixtureUpdateMessage(this, fixture));
        }

        private string GetFixtureName(Fixture fixture)
        {
            if (!string.IsNullOrEmpty(fixture.ID)) return fixture.ID;

            // For any fixtures that aren't named in the code we will assign one.
            return $"fixture-{_fixtures.IndexOf(fixture)}";
        }

        private string GetJointName(Joint joint)
        {
            var id = joint.ID;

            if (!string.IsNullOrEmpty(id)) return id;

            var jointCount = Joints.Count();

            for (var i = 0; i < jointCount + 1; i++)
            {
                id = $"joint-{i}";
                if (GetJoint(id) != null) continue;
                return id;
            }

            Logger.WarningS("physics", $"Unable to get a joint ID; using its hashcode instead.");
            return joint.GetHashCode().ToString();
        }

        /// <summary>
        /// Get a joint with the specified ID.
        /// </summary>
        public Joint? GetJoint(string id)
        {
            foreach (var joint in Joints)
            {
                if (joint.ID == id) return joint;
            }

            return null;
        }

        internal Transform GetTransform()
        {
            return new(Owner.Transform.WorldPosition, (float) Owner.Transform.WorldRotation.Theta);
        }

        public void ApplyLinearImpulse(in Vector2 impulse)
        {
            if ((_bodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0) return;
            Awake = true;

            LinearVelocity += impulse * _invMass;
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

        /// <summary>
        ///     Get the proxies for each of our fixtures and add them to the broadphases.
        /// </summary>
        /// <param name="mapManager"></param>
        /// <param name="broadPhaseSystem"></param>
        public void CreateProxies(IMapManager? mapManager = null, SharedBroadPhaseSystem? broadPhaseSystem = null)
        {
            if (HasProxies) return;

            BroadphaseMapId = Owner.Transform.MapID;

            if (BroadphaseMapId == MapId.Nullspace)
            {
                HasProxies = true;
                return;
            }

            broadPhaseSystem ??= EntitySystem.Get<SharedBroadPhaseSystem>();
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var worldPosition = Owner.Transform.WorldPosition;
            var mapId = Owner.Transform.MapID;
            var worldAABB = GetWorldAABB();
            var worldRotation = Owner.Transform.WorldRotation.Theta;

            // TODO: For singularity and shuttles: Any fixtures that have a MapGrid layer / mask needs to be added to the default broadphase (so it can collide with grids).

            foreach (var gridId in mapManager.FindGridIdsIntersecting(mapId, worldAABB, true))
            {
                var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);
                DebugTools.AssertNotNull(broadPhase);
                if (broadPhase == null) continue; // TODO

                Vector2 offset = worldPosition;
                double gridRotation = worldRotation;

                if (gridId != GridId.Invalid)
                {
                    var grid = mapManager.GetGrid(gridId);
                    offset -= grid.WorldPosition;
                    // TODO: Should probably have a helper for this
                    gridRotation = worldRotation - Owner.EntityManager.GetEntity(grid.GridEntityId).Transform.WorldRotation;
                }

                foreach (var fixture in Fixtures)
                {
                    fixture.ProxyCount = fixture.Shape.ChildCount;
                    var proxies = new FixtureProxy[fixture.ProxyCount];

                    // TODO: Will need to pass in childIndex to this as well
                    for (var i = 0; i < fixture.ProxyCount; i++)
                    {
                        var aabb = fixture.Shape.CalculateLocalBounds(gridRotation).Translated(offset);

                        var proxy = new FixtureProxy(aabb, fixture, i);

                        proxy.ProxyId = broadPhase.AddProxy(ref proxy);
                        proxies[i] = proxy;
                        DebugTools.Assert(proxies[i].ProxyId != DynamicTree.Proxy.Free);
                    }

                    fixture.SetProxies(gridId, proxies);
                }
            }

            HasProxies = true;
        }

        // TOOD: Need SetTransformIgnoreContacts so we can teleport body and /ignore contacts/
        public void DestroyContacts()
        {
            ContactEdge? contactEdge = ContactEdges;
            while (contactEdge != null)
            {
                var contactEdge0 = contactEdge;
                contactEdge = contactEdge.Next;
                PhysicsMap.ContactManager.Destroy(contactEdge0.Contact!);
            }

            ContactEdges = null;
        }

        IEnumerable<IPhysBody> IPhysBody.GetCollidingEntities(Vector2 offset, bool approx)
        {
            return EntitySystem.Get<SharedBroadPhaseSystem>().GetCollidingEntities(this, offset, approx);
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            if (BodyType == BodyType.Static)
            {
                _awake = false;
            }

            Dirty();
            // Yeah yeah TODO Combine these
            // Implicitly assume that stuff doesn't cover if a non-collidable is initialized.

            if (CanCollide)
            {
                if (!Awake)
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsSleepMessage(this));
                }
                else
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsWakeMessage(this));
                }

                if (Owner.IsInContainer())
                {
                    _canCollide = false;
                }
                else
                {
                    // TODO: Probably a bad idea but ehh future sloth's problem; namely that we have to duplicate code between here and CanCollide.
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new CollisionChangeMessage(this, Owner.Uid, _canCollide));
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PhysicsUpdateMessage(this));
                }
            }
        }

        public void ClearJoints()
        {
            for (var je = JointEdges; je != null; je = je.Next)
            {
                RemoveJoint(je.Joint);
            }
        }

        public void AddFixture(Fixture fixture)
        {
            // TODO: SynchronizeFixtures could be more optimally done. Maybe just eventbus it
            // Also we need to queue updates and also not teardown completely every time.
            _fixtures.Add(fixture);
            Dirty();
            EntitySystem.Get<SharedBroadPhaseSystem>().AddFixture(this, fixture);
        }

        public void RemoveFixture(Fixture fixture)
        {
            if (!_fixtures.Contains(fixture))
            {
                Logger.ErrorS("physics", $"Tried to remove fixture that isn't attached to the body {Owner.Uid}");
                return;
            }

            ContactEdge? edge = ContactEdges;
            while (edge != null)
            {
                var contact = edge.Contact!;
                edge = edge.Next;

                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                if (fixture == fixtureA || fixture == fixtureB)
                {
                    PhysicsMap.ContactManager.Destroy(contact);
                }
            }

            _fixtures.Remove(fixture);
            EntitySystem.Get<SharedBroadPhaseSystem>().RemoveFixture(this, fixture);
            ResetMassData();

            Dirty();
        }

        public void AddJoint(Joint joint)
        {
            var id = GetJointName(joint);

            foreach (var existing in Joints)
            {
                // This can happen if a server created joint is sent and applied before the client can create it locally elsewhere
                if (existing.ID.Equals(id)) return;
            }

            PhysicsMap.AddJoint(joint);
            joint.ID = id;
            Logger.DebugS("physics", $"Added joint id: {joint.ID} type: {joint.GetType().Name} to {Owner}");
        }

        public void RemoveJoint(Joint joint)
        {
            PhysicsMap.RemoveJoint(joint);
            Logger.DebugS("physics", $"Removed joint id: {joint.ID} type: {joint.GetType().Name} from {Owner}");
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            // Need to do these immediately in case collision behaviors deleted the body
            // TODO: Could be more optimal as currently broadphase will call this ANYWAY
            DestroyContacts();
            ClearProxies();
            CanCollide = false;
        }

        public void ResetMassData()
        {
            _mass = 0.0f;
            _invMass = 0.0f;
            _inertia = 0.0f;
            InvI = 0.0f;
            // Sweep

            if (((int) _bodyType & (int) BodyType.Kinematic) != 0)
            {
                return;
            }

            var localCenter = Vector2.Zero;

            foreach (var fixture in _fixtures)
            {
                if (fixture.Mass <= 0.0f) continue;

                var fixMass = fixture.Mass;

                _mass += fixMass;
                localCenter += fixture.Centroid * fixMass;
                _inertia += fixture.Inertia;
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

            /* TODO
            // Move center of mass;
            var oldCenter = _sweep.Center;
            _sweep.LocalCenter = localCenter;
            _sweep.Center0 = _sweep.Center = Physics.Transform.Mul(GetTransform(), _sweep.LocalCenter);

            // Update center of mass velocity.
            var a = _sweep.Center - oldCenter;
            _linVelocity += new Vector2(-_angVelocity * a.y, _angVelocity * a.x);
            */
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
            for (var jn = JointEdges; jn != null; jn = jn.Next)
            {
                if (jn.Other != other) continue;
                if (jn.Joint.CollideConnected) continue;
                return false;
            }

            var preventCollideMessage = new PreventCollideEvent(this, other);
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, preventCollideMessage);

            if (preventCollideMessage.Cancelled) return false;

            preventCollideMessage = new PreventCollideEvent(other, this);
            Owner.EntityManager.EventBus.RaiseLocalEvent(other.Owner.Uid, preventCollideMessage);

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
