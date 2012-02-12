using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;

namespace CGO
{
    public class ClickableComponent : GameObjectComponent
    {
        public override ComponentFamily Family
        {
            get { return ComponentFamily.Click; }
        }

        public bool CheckClick(PointF worldPos, out int drawdepth)
        {
            var replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.CheckSpriteClick, replies, worldPos);

            foreach (var reply in replies.Where(reply => reply.MessageType == ComponentMessageType.SpriteWasClicked && (bool)reply.ParamsList[0]))
            {
                drawdepth = (int)reply.ParamsList[1];
                return (bool)reply.ParamsList[0];
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
