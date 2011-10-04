using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

namespace SGO
{

    public class ClickableComponent : GameObjectComponent
    {
        public ClickableComponent()
            :base()
        {
            family = SS3D_shared.GO.ComponentFamily.Click;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            if (message.componentFamily == SS3D_shared.GO.ComponentFamily.Click)
            {
                List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                Owner.SendMessage(this, MessageType.Click, replies, message.messageParameters[0]);
                Owner.HandleClick((int)message.messageParameters[0]);
            }
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);
            return;
        }

        public override void OnRemove()
        {
            base.OnRemove();
        }
    }
}
