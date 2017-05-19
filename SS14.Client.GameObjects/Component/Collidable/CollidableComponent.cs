using Lidgren.Network;
using SFML.Graphics;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Collidable;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidable
    {
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
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "TweakAABB":
                    TweakAABB = parameter.GetValue<Vector4f>() / IoCManager.Resolve<IMapManager>().TileSize;
                    break;
                case "TweakAABBtop":
                    tweakAABB.X = parameter.GetValue<float>() / IoCManager.Resolve<IMapManager>().TileSize;
                    break;
                case "TweakAABBright":
                    tweakAABB.Y = parameter.GetValue<float>() / IoCManager.Resolve<IMapManager>().TileSize;
                    break;
                case "TweakAABBbottom":
                    tweakAABB.Z = parameter.GetValue<float>() / IoCManager.Resolve<IMapManager>().TileSize;
                    break;
                case "TweakAABBleft":
                    tweakAABB.W = parameter.GetValue<float>() / IoCManager.Resolve<IMapManager>().TileSize;
                    break;
                case "DebugColor":
                    DebugColor = ColorUtils.FromHex(parameter.GetValue<string>(), Color.Red);
                    break;
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