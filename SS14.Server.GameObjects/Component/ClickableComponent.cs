using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component
    {
        public ClickableComponent()
        {
            Family = ComponentFamily.Click;
        }

        /// <summary>
        /// NetMessage handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.ComponentFamily == ComponentFamily.Click)
            {
                var type = (ComponentMessageType) message.MessageParameters[0];
                var uid = (int) message.MessageParameters[1];
                if (type == ComponentMessageType.Click)
                    Owner.SendMessage(this, ComponentMessageType.Click, uid);
                else if (type == ComponentMessageType.ClickedInHand)
                    Owner.SendMessage(this, ComponentMessageType.ClickedInHand, uid);
                Owner.RaiseEvent(new ClickedOnEntityEventArgs{Clicked = Owner.Uid, Clicker = uid});
            }
        }
    }
}