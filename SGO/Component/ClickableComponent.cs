using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace SGO
{

    public class ClickableComponent : GameObjectComponent
    {
        public ClickableComponent()
            :base()
        {
            family = SS13_Shared.GO.ComponentFamily.Click;
        }

        /// <summary>
        /// NetMessage handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.componentFamily == SS13_Shared.GO.ComponentFamily.Click)
            {
                Owner.SendMessage(this, ComponentMessageType.Click, message.messageParameters[0]);
            }
        }
    }
}
