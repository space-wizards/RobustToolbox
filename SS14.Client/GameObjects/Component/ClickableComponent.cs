using Lidgren.Network;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects
{
    public class ClickableComponent : ClientComponent
    {
        public override string Name => "Clickable";

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
                    DispatchInHandClick((int)list[0]);
                    break;
            }

            return reply;
        }

        public bool CheckClick(Vector2f worldPos, out int drawdepth)
        {
            ComponentReplyMessage reply = Owner.SendMessage(this, ComponentFamily.Renderable,
                                                            ComponentMessageType.CheckSpriteClick, worldPos);

            if (reply.MessageType == ComponentMessageType.SpriteWasClicked && (bool)reply.ParamsList[0])
            {
                drawdepth = (int)reply.ParamsList[1];
                return (bool)reply.ParamsList[0];
            }

            drawdepth = -1;
            return false;
        }

        public void DispatchClick(int userUID, int clickType)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, MouseClickType.ConvertClickTypeToComponentMessageType(clickType),
                                              userUID);
        }

        public void DispatchInHandClick(int userUID)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                              ComponentMessageType.ClickedInHand, userUID);
        }
    }
}
