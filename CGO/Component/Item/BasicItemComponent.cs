using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace CGO
{
    public class BasicItemComponent : GameObjectComponent
    {
        public BasicItemComponent()
        {
            family = ComponentFamily.Item;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            if (message.componentFamily != family)
                return;
            switch ((ItemComponentNetMessage)message.messageParameters[0])
            {
                case ItemComponentNetMessage.PickedUp://I've been picked up -- says the server's item component
                    Owner.SendMessage(this, ComponentMessageType.PickedUp, null);
                    Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
                    Entity e = EntityManager.Singleton.GetEntity((int)message.messageParameters[1]);
                    Owner.SendMessage(this, ComponentMessageType.SlaveAttach, null, e.Uid);
                    break;
                case ItemComponentNetMessage.Dropped: //I've been dropped -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("NetworkMoverComponent"));
                    Owner.SendMessage(this, ComponentMessageType.Dropped, null);
                    break;
            }
        }
    }
}
