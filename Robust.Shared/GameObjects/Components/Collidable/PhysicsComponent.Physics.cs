using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    public partial interface IPhysicsComponent
    {
        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        float Mass { get; set; }

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        Vector2 Momentum { get; set; }

        /// <summary>
        ///     The current status of the object
        /// </summary>
        BodyStatus Status { get; set; }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        bool OnGround { get; }

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        bool Anchored { get; set; }

        bool Predict { get; set; }

        /// <summary>
        /// Can this body be moved?
        /// </summary>
        /// <returns></returns>
        new bool CanMove();
    }

    partial class PhysicsComponent : IPhysicsComponent
    {
        [ViewVariables]
        public bool HasProxies { get; set; }

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Mass
        {
            get => BodyType == BodyType.Dynamic ? _mass : 0.0f;
            set
            {
                DebugTools.Assert(!float.IsNaN(value));

                if (MathHelper.CloseTo(_mass, value))
                    return;

                // Box2D blocks it if it's dynamic but in case objects can flip-flop between dynamic and static easily via anchoring.
                // So we may as well support it and just guard the InvMass get
                _mass = value;

                if (_mass <= 0.0f)
                    _mass = 1.0f;

                _invMass = 1.0f / _mass;
                Dirty();
            }
        }

        private float _mass;

        /// <summary>
        ///     Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        public float InvMass => BodyType == BodyType.Dynamic ? _invMass : 0.0f;

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
        ///     Scale of gravity applied to this body. Default is 1.
        /// </summary>
        public float GravityScale { get; set; } = 1.0f;

        /// <summary>
        /// Inverse moment of inertia (1 / I).
        /// </summary>
        internal float InvI { get; set; }

        /// <summary>
        ///     Is the body allowed to have angular velocity.
        /// </summary>
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

        private bool _fixedRotation;

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

        private Vector2 _localCenter;

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

        private float _linearDamping;

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

        private float _angularDamping;

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LinearVelocity
        {
            get => _linVelocity;
            set
            {
                DebugTools.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y));

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
                DebugTools.Assert(!float.IsNaN(value));

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
        public BodyStatus Status
        {
            get => _status;
            set
            {
                if (_status == value)
                    return;

                _status = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        public bool OnGround => Status == BodyStatus.OnGround &&
                                !IoCManager.Resolve<IPhysicsManager>()
                                    .IsWeightless(Owner.Transform.Coordinates);

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Obsolete("Use BodyType.Static instead")]
        public bool Anchored
        {
            get => BodyType == BodyType.Static;
            set
            {
                var anchored = BodyType == BodyType.Static;

                if (anchored == value)
                    return;

                if (value)
                {
                    _bodyType = BodyType.Static;
                }
                else
                {
                    _bodyType = BodyType.Dynamic;
                }

                AnchoredChanged?.Invoke();
                SendMessage(new AnchoredChangedMessage(Anchored));
                Dirty();
            }
        }

        [Obsolete("Use AnchoredChangedMessage instead")]
        public event Action? AnchoredChanged;

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

        internal Transform GetTransform()
        {
            return new(Owner.Transform.WorldPosition, (float) Owner.Transform.WorldRotation.Theta);
        }

        public void ApplyLinearImpulse(in Vector2 impulse)
        {
            if (_bodyType != BodyType.Dynamic) return;
            Awake = true;

            LinearVelocity += impulse * _invMass;
        }

        public void ApplyAngularImpulse(float impulse)
        {
            if (_bodyType != BodyType.Dynamic) return;
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
        ///     Calculate our AABB without using proxies.
        /// </summary>
        /// <returns></returns>
        public Box2 GetWorldAABB()
        {
            var mapId = Owner.Transform.MapID;
            if (mapId == MapId.Nullspace)
                return new Box2();

            var worldRotation = Owner.Transform.WorldRotation;
            var bounds = new Box2();

            foreach (var fixture in Fixtures)
            {
                var aabb = fixture.Shape.CalculateLocalBounds(worldRotation);
                bounds = bounds.Union(aabb);
            }

            return bounds.Translated(Owner.Transform.WorldPosition);
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
        public bool CanMove()
        {
            return BodyType == BodyType.Dynamic || (!Anchored && Mass > 0);
        }
    }

    public class AnchoredChangedMessage : ComponentMessage
    {
        public readonly bool Anchored;

        public AnchoredChangedMessage(bool anchored)
        {
            Anchored = anchored;
        }
    }
}
