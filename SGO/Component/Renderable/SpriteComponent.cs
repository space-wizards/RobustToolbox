using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class SpriteComponent : GameObjectComponent
    {
        public SpriteComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Renderable;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.SetSpriteByKey:
                    if (Owner != null) //We got a set sprite message. Forward it on to the clientside sprite components.                    
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.SetSpriteByKey, list[0]);
                    break;
                case ComponentMessageType.SetDrawDepth:
                    if (Owner != null)                  
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.SetDrawDepth, list[0]);
                    break;
            }
        }
    }
}
