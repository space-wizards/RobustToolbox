using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace SGO
{
    public class CollidableComponent : GameObjectComponent
    {
        public CollidableComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Collidable;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.DisableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.DisableCollision);
                    break;
                case ComponentMessageType.EnableCollision:
                    Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.EnableCollision);
                    break;
            }

            return reply;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            switch((ComponentMessageType)message.messageParameters[0])
            {
                case ComponentMessageType.Bumped:
                    ///TODO check who bumped us, how far away they are, etc.
                    Owner.SendMessage(this, ComponentMessageType.Bumped);
                    break;

            }
        }
    }
}
