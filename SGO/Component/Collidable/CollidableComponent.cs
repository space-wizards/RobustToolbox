using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class CollidableComponent : GameObjectComponent
    {
        public CollidableComponent()
        {
            family = ComponentFamily.Collidable;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.DisableCollision:
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                      ComponentMessageType.DisableCollision);
                    break;
                case ComponentMessageType.EnableCollision:
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                      ComponentMessageType.EnableCollision);
                    break;
            }

            return reply;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                case ComponentMessageType.Bumped:
                    ///TODO check who bumped us, how far away they are, etc.
                    Owner.SendMessage(this, ComponentMessageType.Bumped);
                    break;
            }
        }
    }
}