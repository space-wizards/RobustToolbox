using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces;
using ClientServices;
using System.Diagnostics;
using GorgonLibrary;
using SS13_Shared.GO;

namespace CGO
{
    public class CollidableComponent : GameObjectComponent, ICollidable
    {
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

        public CollidableComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Collidable;
        }

        /// <summary>
        /// OnAdd override -- gets the AABB from the sprite component and sends it to the collision manager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            GetAABB();
            ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            cm.AddCollidable(this);
        }

        /// <summary>
        /// OnRemove override -- removes the AABB from the collisionmanager.
        /// </summary>
        public override void OnRemove()
        {
            base.OnRemove();
            ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            cm.RemoveCollidable(this);
        }

        /// <summary>
        /// Message handler -- 
        /// SpriteChanged means the spritecomponent changed the current sprite.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="reply"></param>
        /// <param name="list"></param>
        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);

            switch (type)
            {
                case ComponentMessageType.SpriteChanged:
                    if (collisionEnabled)
                    {
                        GetAABB();
                        ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
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
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];
            switch (type)
            {
                case ComponentMessageType.EnableCollision:
                    EnableCollision();
                    break;
                case ComponentMessageType.DisableCollision:
                    DisableCollision();
                    break;

            }
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
                    if (parameter.Parameter.GetType() == typeof(Vector4D))
                    {
                        TweakAABB = (Vector4D)parameter.Parameter; 
                    }
                    break;
                case "TweakAABBtop":
                    if (parameter.Parameter.GetType() == typeof(string))
                    {
                        tweakAABB.X = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    break;
                case "TweakAABBright":
                    if (parameter.Parameter.GetType() == typeof(string))
                    {
                        tweakAABB.Y = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    break;
                case "TweakAABBbottom":
                    if (parameter.Parameter.GetType() == typeof(string))
                    {
                        tweakAABB.Z = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    break;
                case "TweakAABBleft":
                    if (parameter.Parameter.GetType() == typeof(string))
                    {
                        tweakAABB.W = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    break;
            }
        }

        /// <summary>
        /// Enables collidable
        /// </summary>
        private void EnableCollision()
        {
            collisionEnabled = true;
            ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            cm.AddCollidable(this);
        }

        /// <summary>
        /// Disables Collidable
        /// </summary>
        private void DisableCollision()
        {
            collisionEnabled = false;
            ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            cm.RemoveCollidable(this);
        }

        /// <summary>
        /// Gets the current AABB from the sprite component.
        /// </summary>
        private void GetAABB()
        {
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.GetAABB, replies);
            if (replies.Count > 0 && replies.First().messageType == ComponentMessageType.CurrentAABB)
            {
                currentAABB = (RectangleF)replies.First().paramsList[0];
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
        public void Bump()
        {
            if (OnBump != null)
                OnBump(this, new EventArgs());

            Owner.SendMessage(this, ComponentMessageType.Bumped, null);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, ComponentMessageType.Bumped);
        }


        public bool IsHardCollidable
        { get { return isHardCollidable; } }
        #endregion
    }
}
