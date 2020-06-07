using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private bool _canCollide;
        private BodyStatus _status;
        private BodyType _bodyType;
        private List<IPhysShape> _physShapes = new List<IPhysShape>();

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

        /// <inheritdoc />
        public int ProxyId { get; set; }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _canCollide, "on", true);
            serializer.DataField(ref _status, "Status", BodyStatus.OnGround);
            serializer.DataField(ref _bodyType, "bodyType", BodyType.None);
            serializer.DataField(ref _physShapes, "shapes", new List<IPhysShape>{new PhysShapeAabb()});
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_canCollide, _status, _physShapes);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var newState = (CollidableComponentState)curState;

            _canCollide = newState.CanCollide;
            _status = newState.Status;

            //TODO: Is this always true?
            if (newState.PhysShapes != null)
            {
                _physShapes = newState.PhysShapes;

                foreach (var shape in _physShapes)
                {
                    shape.ApplyState();
                }

                Dirty();
                UpdateEntityTree();
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.WorldAABB
        {
            get
            {
                var pos = Owner.Transform.WorldPosition;
                return ((IPhysBody)this).AABB.Translated(pos);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.AABB
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
        public List<IPhysShape> PhysicsShapes => _physShapes;

        /// <summary>
        ///     Enables or disabled collision processing of this component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanCollide
        {
            get => _canCollide;
            set => _canCollide = value;
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

        [ViewVariables(VVAccess.ReadWrite)]
        public BodyStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // normally ExposeData would create this
            if (_physShapes == null)
                _physShapes = new List<IPhysShape> { new PhysShapeAabb() };
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
            _physicsManager.RemoveBody(this);
            base.Shutdown();
        }

        public bool IsColliding(Vector2 offset)
        {
            return _physicsManager.IsColliding(this, offset);
        }

        public IEnumerable<IEntity> GetCollidingEntities(Vector2 offset)
        {
            return _physicsManager.GetCollidingEntities(this, offset);
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
}
