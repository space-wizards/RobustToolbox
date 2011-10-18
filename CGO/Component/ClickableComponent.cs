using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using System.Drawing;

namespace CGO
{
    public class ClickableComponent : GameObjectComponent
    {
        public ClickableComponent()
            : base()
        {
            family = ComponentFamily.Click;
        }

        public void Clicked(PointF worldPos, int userUID)
        {

            {   //FRANKENCODE.
                
            }
        }

        public bool CheckClick(PointF worldPos, out int drawdepth)
        {
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.CheckSpriteClick, replies, worldPos);
            foreach (var reply in replies)
            {
                if (reply.messageType == ComponentMessageType.SpriteWasClicked && (bool)reply.paramsList[0] == true)
                {
                    drawdepth = (int)reply.paramsList[1];
                    return (bool)reply.paramsList[0];
                }
            }
            drawdepth = -1;
            return false;
        }

        public void DispatchClick(int userUID)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, userUID);
        }
    }
}
