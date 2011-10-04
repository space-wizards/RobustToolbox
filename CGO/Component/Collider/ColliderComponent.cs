using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices;
using ClientInterfaces;
using GorgonLibrary;

namespace CGO
{
    class ColliderComponent : GameObjectComponent
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
            { // Return tweaked AABB
                return new RectangleF(currentAABB.Left + Owner.position.X - (currentAABB.Width / 2) + tweakAABB.W,
                                        currentAABB.Top + Owner.position.Y - (currentAABB.Height / 2) + tweakAABB.X,
                                        currentAABB.Width - (tweakAABB.W - tweakAABB.Y),
                                        currentAABB.Height - (tweakAABB.X - tweakAABB.Z));
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
                    if (list.Count() > 0)
                        reply.Add(CheckCollision((bool)list[0]));
                    else
                        reply.Add(CheckCollision());
                    break;
                    

            }
            return;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "TweakAABB":
                    if (parameter.Parameter.GetType() == typeof(Vector4D))
                        TweakAABB = (Vector4D)parameter.Parameter;
                    break;
            }
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            GetAABB();
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

        private ComponentReplyMessage CheckCollision(bool SuppressBump = false)
        {
            bool isColliding = false;
            ICollisionManager collisionManager = (ICollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
            isColliding = collisionManager.IsColliding(OffsetAABB, SuppressBump);
            return new ComponentReplyMessage(MessageType.CollisionStatus, isColliding);
        }


    }
}
