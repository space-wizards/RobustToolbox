using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class CollidableComponent : GameObjectComponent
    {
        public CollidableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Collidable;
        }

        public override void RecieveMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case MessageType.DisableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, MessageType.DisableCollision);
                    break;
                case MessageType.EnableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, MessageType.EnableCollision);
                    break;
            }
        }
    }
}
