using System;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class EquippableComponent : GameObjectComponent
    {
        public EquipmentSlot wearloc;

        public EquippableComponent()
        {
            family = ComponentFamily.Equippable;
        }

        public Entity currentWearer { get; private set; }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ItemEquipped:
                    HandleEquipped((Entity) list[0]);
                    break;
                case ComponentMessageType.ItemUnEquipped:
                    HandleUnEquipped();
                    break;
                case ComponentMessageType.GetWearLoc:
                    reply = new ComponentReplyMessage(ComponentMessageType.ReturnWearLoc, wearloc);
                    break;
            }

            return reply;
        }

        private void HandleUnEquipped()
        {
            Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("BasicMoverComponent"));
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                              EquippableComponentNetMessage.UnEquipped);
            currentWearer = null;
        }

        private void HandleEquipped(Entity entity)
        {
            currentWearer = entity;
            Owner.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, entity.Uid);
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                              EquippableComponentNetMessage.Equipped, entity.Uid, wearloc);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "wearloc":
                    wearloc = (EquipmentSlot) Enum.Parse(typeof (EquipmentSlot), (string) parameter.Parameter);
                    break;
            }
        }
    }
}