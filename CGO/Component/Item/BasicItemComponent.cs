using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class BasicItemComponent : Component
    {
        public BasicItemComponent()
        {
            Family = ComponentFamily.Item;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            if (message.ComponentFamily != Family)
                return;
            switch ((ItemComponentNetMessage) message.MessageParameters[0])
            {
                case ItemComponentNetMessage.PickedUp: //I've been picked up -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover,
                                       Owner.EntityManager.ComponentFactory.GetComponent("SlaveMoverComponent"));
                    Entity e = Owner.EntityManager.GetEntity((int) message.MessageParameters[1]);
                    var h = (Hand) message.MessageParameters[2];
                    Owner.SendMessage(this, ComponentMessageType.PickedUp, h);
                    Owner.SendMessage(this, ComponentMessageType.SlaveAttach, e.Uid);
                    break;
                case ItemComponentNetMessage.Dropped: //I've been dropped -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover,
                                       Owner.EntityManager.ComponentFactory.GetComponent("NetworkMoverComponent"));
                    Owner.SendMessage(this, ComponentMessageType.Dropped);
                    break;
            }
        }
    }
}