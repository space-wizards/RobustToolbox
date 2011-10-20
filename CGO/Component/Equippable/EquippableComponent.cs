using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace CGO
{
    public class EquippableComponent : GameObjectComponent
    {
        public EquippableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Equippable;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            //base.HandleNetworkMessage(message);

            switch((EquippableComponentNetMessage)message.messageParameters[0])
            {
                case EquippableComponentNetMessage.Equipped:
                    EquippedBy((int)message.messageParameters[1]);
                    break;
                case EquippableComponentNetMessage.UnEquipped:
                    UnEquipped();
                    break;
            }
        }

        private void EquippedBy(int uid)
        {
            Owner.SendMessage(this, ComponentMessageType.ItemEquipped, null);
            Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, null, uid);
        }

        private void UnEquipped()
        {
            Owner.SendMessage(this, ComponentMessageType.ItemUnEquipped, null);
            Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("NetworkMoverComponent"));
        }
    }
}
