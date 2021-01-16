using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Serialization;
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

    public partial interface IPhysicsComponent : IComponent, IPhysBody
    {
        public new bool Hard { get; set; }
        bool IsColliding(Vector2 offset, bool approximate = true);

        IEnumerable<IEntity> GetCollidingEntities(Vector2 offset, bool approximate = true);
        bool UpdatePhysicsTree();

        void RemovedFromPhysicsTree(MapId mapId);
        void AddedToPhysicsTree(MapId mapId);
    }

    public partial class PhysicsComponent : Component, IPhysicsComponent
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private bool _canCollide;
        private bool _isHard;
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
        ///     All of our contacts.
        /// </summary>
        internal List<ContactEdge> ContactEdges { get; set; } = new();

        public IEntity Entity => Owner;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public BodyType BodyType
        {
            get => _bodyType;
            set
            {
                if (_bodyType == value)
                    return;

                _bodyType = value;
                var oldAnchored = _anchored;
                _anchored = _bodyType == BodyType.Static;

                if (oldAnchored != _anchored)
                {
                    AnchoredChanged?.Invoke();
                    SendMessage(new AnchoredChangedMessage(Anchored));
                }
            }
        }

        private BodyType _bodyType;

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
                    Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new PhysicsWakeMessage(this));
                }
                else
                {
                    Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new PhysicsSleepMessage(this));
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
            serializer.DataField(ref _isHard, "hard", true);
            serializer.DataField(ref _status, "status", BodyStatus.OnGround);
            // Farseer defaults this to static buuut knowing our audience most are gonnna forget to set it.
            serializer.DataField(ref _bodyType, "bodyType", BodyType.Dynamic);
            serializer.DataField(ref _fixtures, "fixtures", new List<Fixture>());
            serializer.DataField(ref _anchored, "anchored", true);

            // TODO: Once anchored is just replaced with bodytype we can dump this
            if (_anchored)
            {
                _bodyType = BodyType.Static;
            }

            serializer.DataField(ref _mass, "mass", 1.0f);
            serializer.DataField(ref _awake, "awake", true);
            serializer.DataField(ref _sleepingAllowed, "sleepingAllowed", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_canCollide, _status, _fixtures, _isHard, _mass, LinearVelocity, AngularVelocity, BodyType);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not PhysicsComponentState newState)
                return;

            _canCollide = newState.CanCollide;
            _status = newState.Status;
            _isHard = newState.Hard;
            _fixtures = newState.Fixtures;

            foreach (var fixture in _fixtures)
            {
                fixture.Shape.ApplyState();
            }

            Dirty();
            UpdateEntityTree();
            Mass = newState.Mass / 1000f; // gram to kilogram

            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            BodyType = newState.BodyType;
            Predict = false;
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.WorldAABB
        {
            get
            {
                var pos = Owner.Transform.WorldPosition;
                return ((IPhysBody) this).AABB.Translated(pos);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public Box2 AABB
        {
            get
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var angle = Owner.Transform.WorldRotation;
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
                            offset = mapManager.GetGrid(gridId).WorldToLocal(worldPos);
                        }

                        foreach (var proxy in proxies)
                        {
                            var shapeBounds = proxy.AABB.Translated(offset).CalculateLocalBounds(angle);
                            bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                        }
                    }
                }

                return bounds;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public List<Fixture> Fixtures => _fixtures;

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

                Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, _canCollide));
                Dirty();
            }
        }

        /// <summary>
        ///     Non-hard physics bodies will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Hard
        {
            get => _isHard;
            set
            {
                if (_isHard == value)
                    return;

                _isHard = value;
                Dirty();
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
                    layers = layers | fixture.CollisionLayer;
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
                    mask = mask | fixture.CollisionMask;
                return mask;
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // normally ExposeData would create this
            if (_physShapes == null)
            {
                _physShapes = new List<IPhysShape> {new PhysShapeAabb()};
            }
            else
            {
                foreach (var shape in _physShapes)
                {
                    ShapeAdded(shape);
                }
            }

            foreach (var controller in _controllers.Values)
            {
                controller.ControlledComponent = this;
            }

            Dirty();
            // Yeah yeah TODO Combine these
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, _canCollide));

            if (CanCollide)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsUpdateMessage(this));
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            // In case somebody starts sharing shapes across multiple components I guess?
            foreach (var shape in _physShapes)
            {
                ShapeRemoved(shape);
            }

            // Should we not call this if !_canCollide? PathfindingSystem doesn't care at least.
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, false));
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PhysicsUpdateMessage(this));
        }

        private void ShapeAdded(IPhysShape shape)
        {
            shape.OnDataChanged += ShapeDataChanged;
        }

        private void ShapeRemoved(IPhysShape item)
        {
            item.OnDataChanged -= ShapeDataChanged;
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();
            _physicsManager.AddBody(this);
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            RemoveControllers();
            _physicsManager.RemoveBody(this);
            base.Shutdown();
        }

        public bool IsColliding(Vector2 offset, bool approx = true)
        {
            return _physicsManager.IsColliding(this, offset, approx);
        }

        public IEnumerable<IEntity> GetCollidingEntities(Vector2 offset, bool approx = true)
        {
            return _physicsManager.GetCollidingEntities(this, offset, approx);
        }

        public bool UpdatePhysicsTree()
            => _physicsManager.Update(this);

        public void RemovedFromPhysicsTree(MapId mapId)
        {
            _physicsManager.RemovedFromMap(this, mapId);
        }

        public void AddedToPhysicsTree(MapId mapId)
        {
            _physicsManager.AddedToMap(this, mapId);
        }

        private bool UpdateEntityTree() => Owner.EntityManager.UpdateEntityTree(Owner);

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
}
