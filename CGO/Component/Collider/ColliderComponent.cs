using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices;
using ClientInterfaces;

namespace CGO
{
    class ColliderComponent : GameObjectComponent
    {
        private RectangleF currentAABB;

        private RectangleF OffsetAABB
        {
            get
            {
                return new RectangleF(currentAABB.Left + Owner.position.X - (currentAABB.Width / 2),
                                        currentAABB.Top + Owner.position.Y - (currentAABB.Height / 2),
                                        currentAABB.Width,
                                        currentAABB.Height);
            }
        }

        public ColliderComponent()
            : base()
        { 
            
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            switch (type)
            {
                case MessageType.SpriteChanged:
                    GetAABB();
                    break;
                case MessageType.CheckCollision:
                    reply.Add(CheckCollision());
                    break;
                    

            }
            return;
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            GetAABB();
        }

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

        private ComponentReplyMessage CheckCollision()
        {
            bool isColliding = false;
            ICollisionManager collisionManager = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            isColliding = collisionManager.IsColliding(OffsetAABB);
            return new ComponentReplyMessage(MessageType.CollisionStatus, isColliding);
        }


    }
}
