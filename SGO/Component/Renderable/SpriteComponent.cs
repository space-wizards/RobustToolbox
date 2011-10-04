using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class SpriteComponent : GameObjectComponent
    {
        public SpriteComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Renderable;
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.SetSpriteByKey:
                    //We got a set sprite message. Forward it on to the clientside sprite components.
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, list[0]);
                    break;
            }
        }
    }
}
