using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        private bool _collisionEnabled;
        private bool _isHardCollidable;
        private int _collisionLayer; //bitfield
        private int _collisionMask; //bitfield

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public override Type StateType => typeof(CollidableComponentState);

        /// <inheritdoc />
        Box2 ICollidable.WorldAABB => Owner.GetComponent<BoundingBoxComponent>().WorldAABB;

        /// <inheritdoc />
        Box2 ICollidable.AABB => Owner.GetComponent<BoundingBoxComponent>().AABB;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

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

        /// <inheritdoc />
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

        /// <inheritdoc />
        public bool IsHardCollidable
        {
            get => _isHardCollidable;
            set => _isHardCollidable = value;
        }

        /// <inheritdoc />
        public int CollisionLayer
        {
            get => _collisionLayer;
            set => _collisionLayer = value;
        }

        /// <inheritdoc />
        public int CollisionMask
        {
            get => _collisionMask;
            set => _collisionMask = value;
        }

        /// <summary>
        ///     gets the AABB from the sprite component and sends it to the CollisionManager.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            if (_collisionEnabled)
            {
                var cm = IoCManager.Resolve<IPhysicsManager>();
                cm.AddCollidable(this);
            }
        }

        /// <summary>
        ///     removes the AABB from the CollisionManager.
        /// </summary>
        public override void Shutdown()
        {
            if (_collisionEnabled)
            {
                var cm = IoCManager.Resolve<IPhysicsManager>();
                cm.RemoveCollidable(this);
            }

            base.Shutdown();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (CollidableComponentState) state;

            // edge triggered
            if (newState.CollisionEnabled == _collisionEnabled)
                return;

            if (newState.CollisionEnabled)
                EnableCollision();
            else
                DisableCollision();
        }

        /// <inheritdoc />
        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            return IoCManager.Resolve<IPhysicsManager>().TryCollide(Owner, offset, bump);
        }

        /// <summary>
        ///     Enables collidable
        /// </summary>
        private void EnableCollision()
        {
            _collisionEnabled = true;
            var cm = IoCManager.Resolve<IPhysicsManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        ///     Disables Collidable
        /// </summary>
        private void DisableCollision()
        {
            _collisionEnabled = false;
            var cm = IoCManager.Resolve<IPhysicsManager>();
            cm.RemoveCollidable(this);
        }
    }
}
