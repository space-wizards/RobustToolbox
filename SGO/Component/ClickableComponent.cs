using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO
{
    public class ClickableComponent : GameObjectComponent
    {
        public ClickableComponent()
        {
            family = ComponentFamily.Click;
        }

        /// <summary>
        /// NetMessage handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.componentFamily == ComponentFamily.Click)
            {
                Owner.SendMessage(this, ComponentMessageType.Click, message.messageParameters[0]);
            }
        }
    }
}