using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO
{
    public class SpriteComponent : GameObjectComponent
    {
        private bool visible = true;

        public SpriteComponent()
        {
            family = ComponentFamily.Renderable;
        }

        public bool Visible
        {
            get { return visible; }

            set
            {
                if (value == visible) return;
                visible = value;
                SendVisible(null);
            }
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            base.HandleInstantiationMessage(netConnection);
            SendVisible(netConnection);
        }

        private void SendVisible(NetConnection connection)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, connection,
                                              ComponentMessageType.SetVisible, visible);
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SetSpriteByKey:
                    if (Owner != null)
                        //We got a set sprite message. Forward it on to the clientside sprite components.                    
                        Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                          ComponentMessageType.SetSpriteByKey, list[0]);
                    break;
                case ComponentMessageType.SetVisible:
                    Visible = (bool) list[0];
                    break;
                case ComponentMessageType.SetDrawDepth:
                    if (Owner != null)
                        Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                          ComponentMessageType.SetDrawDepth, list[0]);
                    break;
            }

            return reply;
        }
    }
}