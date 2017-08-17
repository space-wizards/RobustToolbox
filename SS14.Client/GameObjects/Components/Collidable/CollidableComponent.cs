using SFML.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using OpenTK;
using SS14.Shared.Interfaces.Physics;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        public Color DebugColor { get; private set; } = Color.Red;

        // no client side collision support for now
        private bool collisionEnabled = false;

        /// <inheritdoc />
        public override Type StateType => typeof(CollidableComponentState);
        
        /// <inheritdoc />
        Box2 ICollidable.WorldAABB => Owner.GetComponent<BoundingBoxComponent>().WorldAABB;

        /// <inheritdoc />
        Box2 ICollidable.AABB => Owner.GetComponent<BoundingBoxComponent>().AABB;

        /// <summary>
        /// Called when the collidable is bumped into by someone/something
        /// </summary>
        void ICollidable.Bump(IEntity ent)
        {
            OnBump?.Invoke(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
            //Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.Bumped, ent.Uid);
        }

        public bool IsHardCollidable { get; } = true;

        public event EventHandler OnBump;

        /// <summary>
        /// OnAdd override -- gets the AABB from the sprite component and sends it to the collision manager.
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
        /// OnRemove override -- removes the AABB from the collisionmanager.
        /// </summary>
        public override void OnRemove()
        {
            if(collisionEnabled)
            {
                var cm = IoCManager.Resolve<ICollisionManager>();
                cm.RemoveCollidable(this);
            }

            base.OnRemove();
        }

        /// <summary>
        /// Message handler --
        /// SpriteChanged means the spritecomponent changed the current sprite.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="reply"></param>
        /// <param name="list"></param>
        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

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

        /// <summary>
        /// Enables collidable
        /// </summary>
        private void EnableCollision()
        {
            collisionEnabled = true;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        /// Disables Collidable
        /// </summary>
        private void DisableCollision()
        {
            collisionEnabled = false;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.RemoveCollidable(this);
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
    }
}
