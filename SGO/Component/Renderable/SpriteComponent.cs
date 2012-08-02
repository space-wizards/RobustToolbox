using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO
{
    public class SpriteComponent : GameObjectComponent
    {
        private bool visible = true;

        public DrawDepth drawDepth
        {
            get
            {
                return _drawDepth;
            }

            set
            {
                if (value != _drawDepth)
                {
                    _drawDepth = value;
                    SendDrawDepth(null);
                }
            }
        }

        private DrawDepth _drawDepth = DrawDepth.FloorTiles;

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
            SendDrawDepth(netConnection);
        }

        private void SendVisible(NetConnection connection)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, connection,
                                              ComponentMessageType.SetVisible, visible);
        }

        private void SendDrawDepth(NetConnection connection)
        {
            if (drawDepth != DrawDepth.FloorTiles) Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, connection,
                                                           ComponentMessageType.SetDrawDepth, drawDepth);
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
                case ComponentMessageType.SetBaseName:
                    if (Owner != null)
                        Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                            ComponentMessageType.SetBaseName, list[0]);
                    break;
                case ComponentMessageType.SetVisible:
                    Visible = (bool) list[0];
                    break;
                case ComponentMessageType.SetDrawDepth:
                    if (Owner != null)
                        drawDepth = (DrawDepth)list[0];
                        Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                          ComponentMessageType.SetDrawDepth, drawDepth);
                    break;
            }

            return reply;
        }
    }
}