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
            object[] arguments = new object[2];
            arguments[0] = worldPos;
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.Clicked, replies, arguments);
            if ((from reply in replies
                 where reply.messageType == ComponentMessageType.Clicked
                     && (bool)reply.paramsList[0] == true
                 select reply).Count() != 0)
            {   //FRANKENCODE.
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, userUID);
            }
        }
    }
}
