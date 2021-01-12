using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;
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
        private List<IPhysShape> _physShapes = new();

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

        public PhysicsComponent()
        {
            PhysicsShapes = new PhysShapeList(this);
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
            serializer.DataField(ref _physShapes, "shapes", new List<IPhysShape> {new PhysShapeAabb()});
            serializer.DataField(ref _anchored, "anchored", true);

            // TODO: Once anchored is just replaced with bodytype we can dump this
            if (_anchored)
            {
                _bodyType = BodyType.Static;
            }

            serializer.DataField(ref _mass, "mass", 1.0f);
            serializer.DataField(this, x => x.Awake, "awake", false);
            serializer.DataField(this, x => x.SleepingAllowed, "sleepingAllowed", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_canCollide, _status, _physShapes, _isHard, _mass, LinearVelocity, AngularVelocity, Anchored);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not PhysicsComponentState newState)
                return;

            _canCollide = newState.CanCollide;
            _status = newState.Status;
            _isHard = newState.Hard;
            _physShapes = newState.PhysShapes;

            foreach (var shape in _physShapes)
            {
                shape.ApplyState();
            }

            Dirty();
            UpdateEntityTree();
            Mass = newState.Mass / 1000f; // gram to kilogram

            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            Anchored = newState.Anchored;
            // TODO: Does it make sense to reset controllers here?
            // This caused space movement to break in content and I'm not 100% sure this is a good fix.
            // Look man the CM test is in 5 hours cut me some slack.
            //_controllers = null;
            // Reset predict flag to false to avoid predicting stuff too long.
            // Another possibly bad hack for content at the moment.
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
                var angle = Owner.Transform.WorldRotation;
                var bounds = new Box2();

                foreach (var shape in _physShapes)
                {
                    var shapeBounds = shape.CalculateLocalBounds(angle);
                    bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                }

                return bounds;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public IList<IPhysShape> PhysicsShapes { get; }

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

                foreach (var shape in _physShapes)
                    layers = layers | shape.CollisionLayer;
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

                foreach (var shape in _physShapes)
                    mask = mask | shape.CollisionMask;
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

        private void ShapeDataChanged()
        {
            Dirty();
            UpdatePhysicsTree();
        }

        // Custom IList<> implementation so that we can hook addition/removal of shapes.
        // To hook into their OnDataChanged event correctly.
        private sealed class PhysShapeList : IList<IPhysShape>
        {
            private readonly PhysicsComponent _owner;

            public PhysShapeList(PhysicsComponent owner)
            {
                _owner = owner;
            }

            public IEnumerator<IPhysShape> GetEnumerator()
            {
                return _owner._physShapes.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(IPhysShape item)
            {
                _owner._physShapes.Add(item);

                ItemAdded(item);
            }

            public void Clear()
            {
                foreach (var item in _owner._physShapes)
                {
                    ItemRemoved(item);
                }

                _owner._physShapes.Clear();
            }

            public bool Contains(IPhysShape item)
            {
                return _owner._physShapes.Contains(item);
            }

            public void CopyTo(IPhysShape[] array, int arrayIndex)
            {
                _owner._physShapes.CopyTo(array, arrayIndex);
            }

            public bool Remove(IPhysShape item)
            {
                var found = _owner._physShapes.Remove(item);

                if (found)
                {
                    ItemRemoved(item);
                }

                return found;
            }

            public int Count => _owner._physShapes.Count;
            public bool IsReadOnly => false;

            public int IndexOf(IPhysShape item)
            {
                return _owner._physShapes.IndexOf(item);
            }

            public void Insert(int index, IPhysShape item)
            {
                _owner._physShapes.Insert(index, item);
                ItemAdded(item);
            }

            public void RemoveAt(int index)
            {
                var item = _owner._physShapes[index];
                ItemRemoved(item);

                _owner._physShapes.RemoveAt(index);
            }

            public IPhysShape this[int index]
            {
                get => _owner._physShapes[index];
                set
                {
                    var oldItem = _owner._physShapes[index];
                    ItemRemoved(oldItem);

                    _owner._physShapes[index] = value;
                    ItemAdded(value);
                }
            }

            private void ItemAdded(IPhysShape item)
            {
                _owner.ShapeAdded(item);
            }

            public void ItemRemoved(IPhysShape item)
            {
                _owner.ShapeRemoved(item);
            }
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
