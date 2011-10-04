using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces;
using ClientServices;
using System.Diagnostics;

namespace CGO
{
    public class CollidableComponent : GameObjectComponent, ICollidable
    {
        private RectangleF currentAABB;
        private RectangleF OffsetAABB
        {
            get
            {
                if (currentAABB != null)
                    return new RectangleF(currentAABB.Left + Owner.position.X - (currentAABB.Width / 2),
                                        currentAABB.Top + Owner.position.Y - (currentAABB.Height / 2),
                                        currentAABB.Width,
                                        currentAABB.Height);
                else
                    return RectangleF.Empty;
            }
        }
        
        public event EventHandler OnBump;

        private bool collisionEnabled = true;

        public CollidableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Collidable;
        }

        /// <summary>
        /// OnAdd override -- gets the AABB from the sprite component and sends it to the collision manager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
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
        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            switch (type)
            {
                case MessageType.SpriteChanged:
                    if (collisionEnabled)
                    {
                        GetAABB();
                        ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                        cm.UpdateCollidable(this);
                    }
                    break;
                case MessageType.DisableCollision:
                    DisableCollision();
                    break;
                case MessageType.EnableCollision:
                    EnableCollision();
                    break;
            }
        }

        private void EnableCollision()
        {
            collisionEnabled = true;
            ICollisionManager cm = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            cm.AddCollidable(this);
        }

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
            Owner.SendMessage(this, MessageType.GetAABB, replies);
            if (replies.Count > 0 && replies.First().messageType == MessageType.CurrentAABB)
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
        }
        #endregion
    }
}
