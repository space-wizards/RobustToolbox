using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace SGO
{
    public class SpriteComponent : GameObjectComponent
    {
        public bool Visible
        {
            get
            {
                return visible;
            }

            set
            {
                if (value == visible) return;
                visible = value;
                SendVisible(null);
            }
        }

        private bool visible = true;

        public SpriteComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Renderable;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            base.HandleInstantiationMessage(netConnection);
            SendVisible(netConnection);
        }

        private void SendVisible(NetConnection connection)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, connection, ComponentMessageType.SetVisible, visible);
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.SetSpriteByKey:
                    if (Owner != null) //We got a set sprite message. Forward it on to the clientside sprite components.                    
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.SetSpriteByKey, list[0]);
                    break;
                case ComponentMessageType.SetVisible:
                    Visible = (bool)list[0];
                    break;
                case ComponentMessageType.SetDrawDepth:
                    if (Owner != null)                  
                        Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.SetDrawDepth, list[0]);
                    break;
            }
        }
    }
}
