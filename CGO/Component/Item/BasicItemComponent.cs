using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class BasicItemComponent : GameObjectComponent
    {
        public override ComponentFamily Family
        {
            get { return ComponentFamily.Item; }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            if (message.ComponentFamily != Family)
                return;
            switch ((ItemComponentNetMessage)message.MessageParameters[0])
            {
                case ItemComponentNetMessage.PickedUp://I've been picked up -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
                    var e = EntityManager.Singleton.GetEntity((int)message.MessageParameters[1]);
                    var h = (Hand)message.MessageParameters[2];
                    Owner.SendMessage(this, ComponentMessageType.PickedUp, h); 
                    Owner.SendMessage(this, ComponentMessageType.SlaveAttach, e.Uid);
                    break;
                case ItemComponentNetMessage.Dropped: //I've been dropped -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("NetworkMoverComponent"));
                    Owner.SendMessage(this, ComponentMessageType.Dropped);
                    break;
            }
        }
    }
}
