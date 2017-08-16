using OpenTK;
using Lidgren.Network;
using SFML.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Physics;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        public override string Name => "Collidable";
        public override uint? NetID => NetIDs.COLLIDABLE;

        public SFML.Graphics.Color DebugColor { get; set; } = Color.Red;

        private bool collisionEnabled = true;
        private FloatRect currentAABB;
        protected bool isHardCollidable = true;

        public override Type StateType => typeof(CollidableComponentState);

        /// <summary>
        /// X - Top | Y - Right | Z - Bottom | W - Left
        /// </summary>
        private Vector4 TweakAABB { get; set; } = Vector4.Zero;

        private FloatRect OffsetAABB
        {
            get
            {
                if (Owner.TryGetComponent<ITransformComponent>(out var ownerTransform))
                {
                    return
                        new FloatRect(
                            currentAABB.Left +
                            ownerTransform.Position.X -
                            (currentAABB.Width / 2) + TweakAABB.W,
                            currentAABB.Top +
                            ownerTransform.Position.Y -
                            (currentAABB.Height / 2) + TweakAABB.X,
                            currentAABB.Width - (TweakAABB.W - TweakAABB.Y),
                            currentAABB.Height - (TweakAABB.X - TweakAABB.Z));
                }
                else
                {
                    return new FloatRect();
                }
            }
        }

        public FloatRect AABB => OffsetAABB;

        public FloatRect WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                if (trans == null)
                    return AABB;
                return new FloatRect(
                    AABB.Left + trans.Position.X,
                    AABB.Top + trans.Position.Y,
                    AABB.Width,
                    AABB.Height);
            }
        }

        /// <summary>
        /// Called when the collidable is bumped into by someone/something
        /// </summary>
        public void Bump(IEntity ent)
        {
            OnBump?.Invoke(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.Bumped,
                                              ent.Uid);
        }

        public bool IsHardCollidable
        {
            get { return isHardCollidable; }
        }

        public event EventHandler OnBump;

        /// <summary>
        /// OnAdd override -- gets the AABB from the sprite component and sends it to the collision manager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            GetAABB();
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        /// OnRemove override -- removes the AABB from the collisionmanager.
        /// </summary>
        public override void OnRemove()
        {
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.RemoveCollidable(this);

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
                        GetAABB();
                        var cm = IoCManager.Resolve<ICollisionManager>();
                        cm.UpdateCollidable(this);
                    }
                    break;
            }

            return reply;
        }

        /// <summary>
        /// Parameter Setting
        /// Settable params:
        /// TweakAABB - Vector4
        /// </summary>
        public override void LoadParameters(YamlMappingNode mapping)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            YamlNode node;
            if (mapping.TryGetNode("tweakAABB", out node))
            {
                TweakAABB = node.AsVector4() / mapManager.TileSize;
            }

            if (mapping.TryGetNode("TweakAABBtop", out node))
            {
                TweakAABB = new Vector4(node.AsFloat() / mapManager.TileSize, TweakAABB.Y, TweakAABB.Z, TweakAABB.W);
            }

            if (mapping.TryGetNode("TweakAABBright", out node))
            {
                TweakAABB = new Vector4(TweakAABB.X, node.AsFloat() / mapManager.TileSize, TweakAABB.Z, TweakAABB.W);
            }

            if (mapping.TryGetNode("TweakAABBbottom", out node))
            {
                TweakAABB = new Vector4(TweakAABB.X, TweakAABB.Y, node.AsFloat() / mapManager.TileSize, TweakAABB.W);
            }

            if (mapping.TryGetNode("TweakAABBleft", out node))
            {
                TweakAABB = new Vector4(TweakAABB.X, TweakAABB.Y, TweakAABB.Z, node.AsFloat() / mapManager.TileSize);
            }

            if (mapping.TryGetNode("DebugColor", out node))
            {
                DebugColor = ColorUtils.FromHex(node.AsString(), Color.Red);
            }
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

        /// <summary>
        /// Gets the current AABB from the sprite component.
        /// </summary>
        private void GetAABB()
        {
            var component = Owner.GetComponent<ISpriteRenderableComponent>();
            var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            currentAABB = new FloatRect(
                component.AABB.Left / tileSize,
                component.AABB.Top / tileSize,
                component.AABB.Width / tileSize,
                component.AABB.Height / tileSize);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (CollidableComponentState) state;
            if (newState.CollisionEnabled == collisionEnabled)
                return;

            if (newState.CollisionEnabled)
                EnableCollision();
            else
                DisableCollision();
        }
    }
}
