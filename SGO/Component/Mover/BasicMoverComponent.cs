using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO
{
    class BasicMoverComponent : GameObjectComponent
    {
        public BasicMoverComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SendPositionUpdate:
                    SendPositionUpdate(true);
                    break;
            }
            return reply;
        }

        public void Translate(double x, double y)
        {
            Vector2 oldPosition = Owner.position;
            Owner.position = new Vector2(x, y);
            Owner.Moved(oldPosition);
            SendPositionUpdate();
        }

        public void SendPositionUpdate(bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, Owner.position.X, Owner.position.Y, forced);
        }

        public void SendPositionUpdate(NetConnection client, bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, client, Owner.position.X, Owner.position.Y, forced);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendPositionUpdate(netConnection, true);
        }
    }
}
