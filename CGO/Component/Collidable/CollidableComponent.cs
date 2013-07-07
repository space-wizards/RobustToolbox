using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Collision;
using ClientInterfaces.GOC;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Collidable;

namespace CGO
{
    public class CollidableComponent : GameObjectComponent, ICollidable
    {
        public override ComponentFamily Family
        {
            get { return ComponentFamily.Collidable; }
        }

        /// <summary>
        /// X - Top | Y - Right | Z - Bottom | W - Left
        /// </summary>
        private Vector4D tweakAABB = Vector4D.Zero;
        private Vector4D TweakAABB
        {
            get { return tweakAABB; }
            set { tweakAABB = value; }
        }

        private RectangleF currentAABB;
        private RectangleF OffsetAABB
        {
            get
            {// Return tweaked AABB
                if (currentAABB != null)
                    return new RectangleF(currentAABB.Left + Owner.Position.X - (currentAABB.Width / 2) + tweakAABB.W,
                                        currentAABB.Top + Owner.Position.Y - (currentAABB.Height / 2) + tweakAABB.X,
                                        currentAABB.Width - (tweakAABB.W - tweakAABB.Y),
                                        currentAABB.Height - (tweakAABB.X - tweakAABB.Z));
                else
                    return RectangleF.Empty;
            }
        }
        
        public event EventHandler OnBump;

        private bool collisionEnabled = true;
        protected bool isHardCollidable = true;

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
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

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
        /// TweakAABB - Vector4D
        /// </summary>
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "TweakAABB":
                    TweakAABB = parameter.GetValue<Vector4D>(); 
                    break;
                case "TweakAABBtop":
                    tweakAABB.X = parameter.GetValue<float>();
                    break;
                case "TweakAABBright":
                    tweakAABB.Y = parameter.GetValue<float>();
                    break;
                case "TweakAABBbottom":
                    tweakAABB.Z = parameter.GetValue<float>();
                    break;
                case "TweakAABBleft":
                    tweakAABB.W = parameter.GetValue<float>();
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
            var reply = Owner.SendMessage(this, ComponentFamily.Renderable, ComponentMessageType.GetAABB);
            if (reply.MessageType == ComponentMessageType.CurrentAABB)
            {
                currentAABB = (RectangleF)reply.ParamsList[0];
            }
            else
                return;
        }

        #region ICollidable Members
        public System.Drawing.RectangleF AABB
        {
            get { return OffsetAABB; }
        }

        /// <summary>
        /// Called when the collidable is bumped into by someone/something
        /// </summary>
        public void Bump(IEntity ent)
        {
            if (OnBump != null)
                OnBump(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.Bumped, ent.Uid);
        }


        public bool IsHardCollidable
        { get { return isHardCollidable; } }
        #endregion

        public override void HandleComponentState(ComponentState state)
        {
            var collidableState = (CollidableComponentState) state;
            if(collidableState.CollisionEnabled != collisionEnabled)
            {
                if(collidableState.CollisionEnabled) 
                    EnableCollision();
                else
                    DisableCollision();
            }
        }
    }
}
