using Lidgren.Network;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects
{
    public class ClickableComponent : ClientComponent
    {
        public override string Name => "Clickable";

        public override uint? NetID => NetIDs.CLICKABLE;

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
            var component = Owner.GetComponent<IClickTargetComponent>();

            drawdepth = (int)component.DrawDepth;
            return component.WasClicked(worldPos);
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
