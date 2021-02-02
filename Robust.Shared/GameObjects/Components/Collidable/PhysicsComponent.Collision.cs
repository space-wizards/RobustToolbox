using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public interface ICollideBehavior
    {
        void CollideWith(IEntity collidedWith);

        /// <summary>
        ///     Called after all collisions have been processed, as well as how many collisions occured
        /// </summary>
        /// <param name="collisionCount"></param>
        void PostCollide(int collisionCount) { }
    }

    public interface ICollideSpecial
    {
        bool PreventCollide(IPhysBody collidedwith);
    }

    // TODO: Remove.
    public partial interface IPhysicsComponent : IComponent, IPhysBody
    {
        public new bool Hard { get; set; }
    }

    // TODO: Merge IPhysBody and IPhysicsComponent
    [ComponentReference(typeof(IPhysicsComponent))]
    public partial class PhysicsComponent : Component
    {
        private BodyStatus _status;

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
        /// </summary>
        public int IslandIndex { get; set; }

        /// <summary>
        ///     Linked-list of all of our contacts.
        /// </summary>
        internal ContactEdge? ContactEdges { get; set; } = default!;

        public IEntity Entity => Owner;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

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

                Awake = false;
                var oldAnchored = _bodyType == BodyType.Static;
                _bodyType = value;
                var anchored = _bodyType == BodyType.Static;

                if (oldAnchored != anchored)
                {
                    AnchoredChanged?.Invoke();
                    SendMessage(new AnchoredChangedMessage(Anchored));
                }
            }
        }

        private BodyType _bodyType;

        // We'll also block Static bodies from ever being awake given they don't need to move.
        /// <inheritdoc />
        [ViewVariables]
        public bool Awake
        {
            get => _awake;
            set
            {
                if (_awake == value)
                    return;

                _awake = value;
                _sleepTime = 0.0f;

                if (_awake)
                {
                    // TODO: Lot more farseer shit here
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsWakeMessage(this));
                }
                else
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsSleepMessage(this));
                }

                _linVelocity = Vector2.Zero;
                _angVelocity = 0.0f;
                Dirty();
            }
        }

        private bool _awake;

        public bool SleepingAllowed
        {
            get => _sleepingAllowed;
            set
            {
                if (_sleepingAllowed == value)
                    return;

                _sleepingAllowed = value;

                if (_sleepingAllowed)
                    Awake = true;
            }
        }

        private bool _sleepingAllowed;

        public float SleepTime
        {
            get => _sleepTime;
            set
            {
                if (MathHelper.CloseTo(value, _sleepTime))
                    return;

                _sleepTime = value;
            }
        }

        private float _sleepTime;

        /// <inheritdoc />
        [Obsolete("Set Awake directly")]
        public void WakeBody()
        {
            Awake = true;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _canCollide, "on", true);
            serializer.DataField(ref _status, "status", BodyStatus.OnGround);
            // Farseer defaults this to static buuut knowing our audience most are gonnna forget to set it.
            serializer.DataField(ref _bodyType, "bodyType", BodyType.Dynamic);
            serializer.DataReadWriteFunction("fixtures", new List<Fixture>(), fixtures =>
            {
                foreach (var fixture in fixtures)
                {
                    fixture.Body = this;
                    _fixtures.Add(fixture);
                }
            }, () => Fixtures);

            // TODO: Dump someday
            serializer.DataReadFunction("anchored", true, value =>
            {
                _bodyType = value ? BodyType.Static : BodyType.Dynamic;
            });

            serializer.DataField(ref _linearDamping, "linearDamping", 0.8f);
            serializer.DataField(ref _angularDamping, "angularDamping", 0.2f);
            serializer.DataField(ref _mass, "mass", 1.0f);
            serializer.DataField(ref _awake, "awake", true);
            serializer.DataField(ref _sleepingAllowed, "sleepingAllowed", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            var fixtureData = new List<FixtureData>();

            foreach (var fixture in _fixtures)
            {
                fixtureData.Add(FixtureData.From(fixture));
            }

            return new PhysicsComponentState(_canCollide, _status, fixtureData, _mass, LinearVelocity, AngularVelocity, BodyType);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not PhysicsComponentState newState)
                return;

            CanCollide = newState.CanCollide;
            Status = newState.Status;

            var toAdd = new List<Fixture>();
            var toRemove = new List<Fixture>();

            // TODO: This diffing is crude (muh ordering) but at least it will save the broadphase updates 90% of the time.
            for (var i = 0; i < newState.Fixtures.Count; i++)
            {
                var newFixture = FixtureData.To(newState.Fixtures[i]);
                newFixture.Body = this;

                // Existing fixture
                if (_fixtures.Count > i)
                {
                    var existingFixture = _fixtures[i];

                    if (!existingFixture.Equals(newFixture))
                    {
                        toRemove.Add(existingFixture);
                        toAdd.Add(newFixture);
                    }
                }
                else
                {
                    toAdd.Add(newFixture);
                }
            }

            foreach (var fixture in toRemove)
            {
                RemoveFixture(fixture);
            }

            foreach (var fixture in toAdd)
            {
                AddFixture(fixture);
                fixture.Shape.ApplyState();
            }

            Dirty();
            // TODO: Should transform just be doing this??? UpdateEntityTree();
            Mass = newState.Mass / 1000f; // gram to kilogram

            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            BodyType = newState.BodyType;
            Predict = false;
        }

        public Box2 GetWorldAABB(IMapManager? mapManager)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var bounds = new Box2();

            foreach (var fixture in _fixtures)
            {
                foreach (var (gridId, proxies) in fixture.Proxies)
                {
                    Vector2 offset;

                    if (gridId == GridId.Invalid)
                    {
                        offset = Vector2.Zero;
                    }
                    else
                    {
                        offset = mapManager.GetGrid(gridId).WorldPosition;
                    }

                    foreach (var proxy in proxies)
                    {
                        var shapeBounds = proxy.AABB.Translated(offset);
                        bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                    }
                }
            }

            return bounds.IsEmpty() ? Box2.UnitCentered.Translated(Owner.Transform.WorldPosition) : bounds;
        }

        /// <inheritdoc />
        [ViewVariables]
        public Box2 AABB
        {
            get
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var worldPos = Owner.Transform.WorldPosition;
                var bounds = new Box2();

                foreach (var fixture in _fixtures)
                {
                    foreach (var (gridId, proxies) in fixture.Proxies)
                    {
                        Vector2 offset;

                        if (gridId == GridId.Invalid)
                        {
                            offset = Vector2.Zero;
                        }
                        else
                        {
                            offset = mapManager.GetGrid(gridId).WorldPosition;
                        }

                        foreach (var proxy in proxies)
                        {
                            var shapeBounds = proxy.AABB.Translated(offset);
                            bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                        }
                    }
                }

                // Get it back in local-space.
                return bounds.Translated(-worldPos);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public IReadOnlyList<Fixture> Fixtures => _fixtures;

        private List<Fixture> _fixtures = new();

        /// <summary>
        ///     Enables or disabled collision processing of this component.
        /// </summary>
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
                Dirty();
            }
        }

        private bool _canCollide;

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

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            if (BodyType == BodyType.Static)
            {
                _awake = false;
            }

            foreach (var controller in _controllers.Values)
            {
                controller.ControlledComponent = this;
            }

            Dirty();
            // Yeah yeah TODO Combine these
            // Implicitly assume that stuff doesn't cover if a non-collidable is initialized.

            if (CanCollide)
            {
                if (!Owner.IsInContainer())
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(this, Owner.Uid, _canCollide));
                }
                else
                {
                    _canCollide = false;
                }
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsUpdateMessage(this));
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
            if (!_fixtures.Remove(fixture))
            {
                Logger.WarningS("physics", $"Tried to remove fixture that isn't attached to the body {Owner.Uid}");
                return;
            }

            Dirty();
            EntitySystem.Get<SharedBroadPhaseSystem>().RemoveFixture(this, fixture);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            // Should we not call this if !_canCollide? PathfindingSystem doesn't care at least.
            // TODO: Suss out if this best way to do it; tl;dr is if we cache it then body is probably deleted by the time we get to it (and its MapId is no longer valid).
            EntitySystem.Get<SharedBroadPhaseSystem>().RemoveBody(this);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsUpdateMessage(this));
        }

        /// <summary>
        ///     Used to prevent bodies from colliding; may lie depending on joints.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal bool ShouldCollide(PhysicsComponent other)
        {
            // At least one body should be dynamic.
            if (_bodyType != BodyType.Dynamic && other._bodyType != BodyType.Dynamic)
            {
                return false;
            }

            // Does a joint prevent collision?
            /*
            for (JointEdge jn = JointList; jn != null; jn = jn.Next)
            {
                if (jn.Other == other)
                {
                    if (jn.Joint.CollideConnected == false)
                    {
                        return false;
                    }
                }
            }
            */

            return true;
        }

        public bool IsOnGround()
        {
            return Status == BodyStatus.OnGround;
        }

        public bool IsInAir()
        {
            return Status == BodyStatus.InAir;
        }
    }

    [Serializable, NetSerializable]
    public enum BodyStatus: byte
    {
        OnGround,
        InAir
    }

    /// <summary>
    ///     Sent whenever a <see cref="IPhysicsComponent"/> is changed.
    /// </summary>
    public sealed class PhysicsUpdateMessage : EntitySystemMessage
    {
        public PhysicsComponent Component { get; }

        public PhysicsUpdateMessage(PhysicsComponent component)
        {
            Component = component;
        }
    }

    public sealed class FixtureUpdateMessage : EntitySystemMessage
    {
        public PhysicsComponent Body { get; }

        public Fixture Fixture { get; }

        public FixtureUpdateMessage(PhysicsComponent body, Fixture fixture)
        {
            Body = body;
            Fixture = fixture;
        }
    }
}
