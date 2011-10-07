using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class CollidableComponent : GameObjectComponent
    {
        public CollidableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Collidable;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.DisableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.DisableCollision);
                    break;
                case ComponentMessageType.EnableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.EnableCollision);
                    break;
            }
        }
    }
}
