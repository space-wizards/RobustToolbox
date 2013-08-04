using System.Drawing;
using GameObject;
using Lidgren.Network;
using SS13_Shared.GO;

namespace CGO
{
    public class ClickableComponent : Component
    {
        public ClickableComponent()
        {
            Family = ComponentFamily.Click;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);
            if (sender == this)
                return reply;

            switch (type)
            {
                case ComponentMessageType.ClickedInHand:
                    DispatchInHandClick((int) list[0]);
                    break;
            }

            return reply;
        }

        public bool CheckClick(PointF worldPos, out int drawdepth)
        {
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Renderable,
                                                            ComponentMessageType.CheckSpriteClick, worldPos);

            if (reply.MessageType == ComponentMessageType.SpriteWasClicked && (bool) reply.ParamsList[0])
            {
                drawdepth = (int) reply.ParamsList[1];
                return (bool) reply.ParamsList[0];
            }

            drawdepth = -1;
            return false;
        }

        public void DispatchClick(int userUID)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, ComponentMessageType.Click,
                                              userUID);
        }

        public void DispatchInHandClick(int userUID)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.ClickedInHand, userUID);
        }
    }
}