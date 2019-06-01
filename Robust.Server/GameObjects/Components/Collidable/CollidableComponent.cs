using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private bool _collisionEnabled;
        private bool _isHardCollidable;
        private int _collisionLayer;
        private int _collisionMask;
        private bool _isScrapingFloor;
        private PhysShapeAabbComp _physShapeAabb;

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _collisionEnabled, "on", true);
            serializer.DataField(ref _isHardCollidable, "hard", true);
            serializer.DataField(ref _collisionLayer, "layer", 1);
            serializer.DataField(ref _collisionMask, "mask", 1);
            serializer.DataField(ref _isScrapingFloor, "IsScrapingFloor", false);
            serializer.DataField(ref _physShapeAabb, "shape", new PhysShapeAabbComp(Owner));
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_collisionEnabled, _isHardCollidable, _collisionLayer, _collisionMask);
        }

        /// <inheritdoc />
        Box2 ICollidable.WorldAABB
        {
            get
            {
                var angle = Owner.Transform.WorldRotation;
                var pos = Owner.Transform.WorldPosition;
                return _physShapeAabb.CalculateLocalBounds(angle).Translated(pos);
            }
        }

        /// <inheritdoc />
        Box2 ICollidable.AABB
        {
            get
            {
                var angle = Owner.Transform.WorldRotation;
                return _physShapeAabb.CalculateLocalBounds(angle);
            }
        }

        /// <inheritdoc />
        public IPhysShape PhysicsShape => _physShapeAabb;

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
            get => _collisionLayer;
            set => _collisionLayer = value;
        }

        /// <summary>
        ///     Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => _collisionMask;
            set => _collisionMask = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsScrapingFloor
        {
            get => _isScrapingFloor;
            set => _isScrapingFloor = value;
        }

        /// <inheritdoc />
        void ICollidable.Bumped(IEntity bumpedby)
        {
            SendMessage(new BumpedEntMsg(bumpedby));
        }

        /// <inheritdoc />
        void ICollidable.Bump(List<IEntity> bumpedinto)
        {
            var collidecomponents = Owner.GetAllComponents<ICollideBehavior>().ToList();

            for (var i = 0; i < collidecomponents.Count; i++)
            {
                collidecomponents[i].CollideWith(bumpedinto);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _physShapeAabb.Entity = Owner;
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();

            _physicsManager.AddCollidable(this);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _physicsManager.RemoveCollidable(this);

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
