using System;
using OpenTK;
using SFML.Graphics;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        // no client side collision support for now
        private bool collisionEnabled;

        public Color DebugColor { get; } = Color.Red;

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

        /// <summary>
        ///     Called when the collidable is bumped into by someone/something
        /// </summary>
        void ICollidable.Bump(IEntity ent)
        {
            OnBump?.Invoke(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
        }

        /// <inheritdoc />
        public bool IsHardCollidable { get; } = true;

        /// <summary>
        ///     gets the AABB from the sprite component and sends it to the CollisionManager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);

            if (collisionEnabled)
            {
                var cm = IoCManager.Resolve<ICollisionManager>();
                cm.AddCollidable(this);
            }
        }

        /// <summary>
        ///     removes the AABB from the CollisionManager.
        /// </summary>
        public override void OnRemove()
        {
            if (collisionEnabled)
            {
                var cm = IoCManager.Resolve<ICollisionManager>();
                cm.RemoveCollidable(this);
            }

            base.OnRemove();
        }

        /// <summary>
        ///     SpriteChanged means the spritecomponent changed the current sprite.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="reply"></param>
        /// <param name="list"></param>
        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
            params object[] list)
        {
            var reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SpriteChanged:
                    if (collisionEnabled)
                    {
                        var cm = IoCManager.Resolve<ICollisionManager>();
                        cm.UpdateCollidable(this);
                    }
                    break;
            }

            return reply;
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (CollidableComponentState) state;

            // edge triggered
            if (newState.CollisionEnabled == collisionEnabled)
                return;

            if (newState.CollisionEnabled)
                EnableCollision();
            else
                DisableCollision();
        }

        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }

        public event EventHandler OnBump;

        /// <summary>
        ///     Enables collidable
        /// </summary>
        private void EnableCollision()
        {
            collisionEnabled = true;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        ///     Disables Collidable
        /// </summary>
        private void DisableCollision()
        {
            collisionEnabled = false;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.RemoveCollidable(this);
        }
    }
}
