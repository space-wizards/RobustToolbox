using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;

namespace SGO
{
    internal class BasicMoverComponent : GameObjectComponent
    {
        public BasicMoverComponent()
        {
            family = ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

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
            Vector2 oldPosition = Owner.Position;
            Owner.Position = new Vector2(x, y);
            Owner.Moved(oldPosition);
            SendPositionUpdate();
        }

        public void SendPositionUpdate(bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, Owner.Position.X,
                                              Owner.Position.Y, forced);
        }

        public void SendPositionUpdate(NetConnection client, bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client, Owner.Position.X,
                                              Owner.Position.Y, forced);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendPositionUpdate(netConnection, true);
        }

        public override ComponentState GetComponentState()
        {
            return new MoverComponentState(Owner.Position.X, Owner.Position.Y);
        }
    }
}