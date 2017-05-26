using Lidgren.Network;
using SFML.Graphics;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Collidable;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class CollidableComponent : Component, ICollidable
    {
        public override string Name => "Collidable";

        public SFML.Graphics.Color DebugColor { get; set; }

        private bool collisionEnabled = true;
        private FloatRect currentAABB;
        protected bool isHardCollidable = true;

        /// <summary>
        /// X - Top | Y - Right | Z - Bottom | W - Left
        /// </summary>
        private Vector4f tweakAABB;

        public CollidableComponent()
        {
            Family = ComponentFamily.Collidable;
            DebugColor = Color.Red;
            tweakAABB = new Vector4f(0,0,0,0);
        }

        public override Type StateType
        {
            get { return typeof (CollidableComponentState); }
        }

        private Vector4f TweakAABB
        {
            get { return tweakAABB; }
            set { tweakAABB = value; }
        }

        private FloatRect OffsetAABB
        {
            get
            {
                var ownerTransform = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);

                // Return tweaked AABB
                if (currentAABB != null && ownerTransform != null)
                {
                    return
                        new FloatRect(
                            currentAABB.Left +
                            ownerTransform.Position.X -
                            (currentAABB.Width / 2) + tweakAABB.W,
                            currentAABB.Top +
                            ownerTransform.Position.Y -
                            (currentAABB.Height / 2) + tweakAABB.X,
                            currentAABB.Width - (tweakAABB.W - tweakAABB.Y),
                            currentAABB.Height - (tweakAABB.X - tweakAABB.Z));
                }
                else
                {
                    return new FloatRect();
                }
            }
        }

        #region ICollidable Members

        public FloatRect AABB
        {
            get { return OffsetAABB; }
        }

        /// <summary>
        /// Called when the collidable is bumped into by someone/something
        /// </summary>
        public void Bump(Entity ent)
        {
            if (OnBump != null)
                OnBump(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.Bumped,
                                              ent.Uid);
        }


        public bool IsHardCollidable
        {
            get { return isHardCollidable; }
        }

        #endregion

        public event EventHandler OnBump;

        /// <summary>
        /// OnAdd override -- gets the AABB from the sprite component and sends it to the collision manager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(Entity owner)
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
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

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
                case ComponentMessageType.DisableCollision:
                    DisableCollision();
                    break;
                case ComponentMessageType.EnableCollision:
                    EnableCollision();
                    break;
            }

            return reply;
        }

        /// <summary>
        /// Parameter Setting
        /// Settable params:
        /// TweakAABB - Vector4
        /// </summary>
        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            YamlNode node;
            if (mapping.TryGetValue("tweakAABB", out node))
            {
                TweakAABB = node.AsVector4f() / mapManager.TileSize;
            }

            if (mapping.TryGetValue("TweakAABBtop", out node))
            {
                tweakAABB.X = node.AsFloat() / mapManager.TileSize;
            }

            if (mapping.TryGetValue("TweakAABBright", out node))
            {
                tweakAABB.Y = node.AsFloat() / mapManager.TileSize;
            }

            if (mapping.TryGetValue("TweakAABBbottom", out node))
            {
                tweakAABB.Z = node.AsFloat() / mapManager.TileSize;
            }

            if (mapping.TryGetValue("TweakAABBleft", out node))
            {
                tweakAABB.W = node.AsFloat() / mapManager.TileSize;
            }

            if (mapping.TryGetValue("DebugColor", out node))
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
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Renderable,
                                                            ComponentMessageType.GetAABB);
            if (reply.MessageType == ComponentMessageType.CurrentAABB)
            {
                var tileSize = IoCManager.Resolve<IMapManager>().TileSize;
                currentAABB = (FloatRect) reply.ParamsList[0];
                currentAABB = new FloatRect(
                    currentAABB.Left / tileSize,
                    currentAABB.Top / tileSize,
                    currentAABB.Width / tileSize,
                    currentAABB.Height / tileSize);
            }
            else
                return;
        }

        public override void HandleComponentState(dynamic state)
        {
            if (state.CollisionEnabled != collisionEnabled)
            {
                if (state.CollisionEnabled)
                    EnableCollision();
                else
                    DisableCollision();
            }
        }
    }
}
