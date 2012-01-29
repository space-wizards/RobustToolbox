using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class EquippableComponent : GameObjectComponent
    {
        public Entity currentWearer { get; private set; }
        public EquipmentSlot wearloc;

        public EquippableComponent()
        {
            family = ComponentFamily.Equippable;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.ItemEquipped:
                    HandleEquipped((Entity)list[0]);
                    break;
                case ComponentMessageType.ItemUnEquipped:
                    HandleUnEquipped();
                    break;
                case ComponentMessageType.GetWearLoc:
                    replies.Add(new ComponentReplyMessage(ComponentMessageType.ReturnWearLoc, wearloc));
                    break;
            }
        }

        private void HandleUnEquipped()
        {
            Owner.AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("BasicMoverComponent"));
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, EquippableComponentNetMessage.UnEquipped);
            currentWearer = null;
        }

        private void HandleEquipped(Entity entity)
        {
            currentWearer = entity;
            Owner.AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, null, entity.Uid);
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, EquippableComponentNetMessage.Equipped, entity.Uid, wearloc);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch(parameter.MemberName)
            {
                case "wearloc":
                    wearloc = (EquipmentSlot)Enum.Parse(typeof(EquipmentSlot), (string)parameter.Parameter);
                    break;
            }
        }
    }
}
