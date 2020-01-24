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
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private bool _collisionEnabled;
        private bool _isHardCollidable;
        private bool _isScrapingFloor;
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

            serializer.DataField(ref _collisionEnabled, "on", true);
            serializer.DataField(ref _isHardCollidable, "hard", true);
            serializer.DataField(ref _isScrapingFloor, "IsScrapingFloor", false);
            serializer.DataField(ref _bodyType, "bodyType", BodyType.None);
            serializer.DataField(ref _physShapes, "shapes", new List<IPhysShape>{new PhysShapeAabb()});
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_collisionEnabled, _isHardCollidable, _isScrapingFloor, _physShapes);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var newState = (CollidableComponentState)curState;

            _collisionEnabled = newState.CollisionEnabled;
            _isHardCollidable = newState.HardCollidable;
            _isScrapingFloor = newState.ScrapingFloor;

            //TODO: Is this always true?
            if (newState.PhysShapes != null)
            {
                _physShapes = newState.PhysShapes;

                foreach (var shape in _physShapes)
                {
                    shape.ApplyState();
                }
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
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsHardCollidable
        {
            get => _isHardCollidable;
            set => _isHardCollidable = value;
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
        public bool IsScrapingFloor
        {
            get => _isScrapingFloor;
            set => _isScrapingFloor = value;
        }

        /// <inheritdoc />
        void IPhysBody.Bumped(IEntity bumpedby)
        {
            SendMessage(new BumpedEntMsg(bumpedby));
        }

        /// <inheritdoc />
        void IPhysBody.Bump(List<IEntity> bumpedinto)
        {
            var collidecomponents = Owner.GetAllComponents<ICollideBehavior>().ToList();

            for (var i = 0; i < collidecomponents.Count; i++)
            {
                collidecomponents[i].CollideWith(bumpedinto);
            }
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

        /// <inheritdoc />
        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            if (!_collisionEnabled || CollisionMask == 0x0)
                return false;

            return _physicsManager.TryCollide(Owner, offset, bump);
        }
    }
}
