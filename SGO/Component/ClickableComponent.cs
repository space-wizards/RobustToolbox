using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using SS3D_shared.GO;
using Lidgren.Network;

namespace SGO
{

    public class ClickableComponent : GameObjectComponent
    {
        public ClickableComponent()
            :base()
        {
            family = SS3D_shared.GO.ComponentFamily.Click;
        }

        /// <summary>
        /// NetMessage handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.componentFamily == SS3D_shared.GO.ComponentFamily.Click)
            {
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                Owner.SendMessage(this, ComponentMessageType.Click, replies, message.messageParameters[0]);
                //Who clicked us?
                
                //parameter 0 is id of clicker
                //Owner.HandleClick((int)message.messageParameters[0]);
            }
        }
    }
}
