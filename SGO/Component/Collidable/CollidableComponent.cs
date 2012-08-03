using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;

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
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                      ComponentMessageType.DisableCollision);
                    break;
                case ComponentMessageType.EnableCollision:
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
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
                    IEntity bumper = EntityManager.Singleton.GetEntity((int) message.MessageParameters[1]);
                    if(bumper != null)
                        Owner.SendMessage(this, ComponentMessageType.Bumped, bumper);
                    break;
            }
        }
    }
}