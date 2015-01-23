using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Equippable;
using System;

namespace SS14.Server.GameObjects
{
    public class EquippableComponent : Component
    {
        public EquipmentSlot wearloc;

        public Entity currentWearer { get; set; }

        public EquippableComponent()
        {
            Family = ComponentFamily.Equippable;
        }

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
            Owner.AddComponent(ComponentFamily.Mover,
                               Owner.EntityManager.ComponentFactory.GetComponent("BasicMoverComponent"));
            /*Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                      EquippableComponentNetMessage.UnEquipped);*/
            currentWearer = null;
        }

        private void HandleEquipped(Entity entity)
        {
            currentWearer = entity;
            Owner.AddComponent(ComponentFamily.Mover,
                               Owner.EntityManager.ComponentFactory.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, entity.Uid);
           /* Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                                      EquippableComponentNetMessage.Equipped, entity.Uid, wearloc);*/
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "wearloc":
                    wearloc = (EquipmentSlot) Enum.Parse(typeof (EquipmentSlot), parameter.GetValue<string>());
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new EquippableComponentState(wearloc, currentWearer != null ? currentWearer.Uid : (int?)null);
        }
    }
}