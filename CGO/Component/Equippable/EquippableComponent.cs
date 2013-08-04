using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class EquippableComponent : Component
    {
        public EquippableComponent()
        {
            Family = ComponentFamily.Equippable;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            //base.HandleNetworkMessage(message);

            switch ((EquippableComponentNetMessage) message.MessageParameters[0])
            {
                case EquippableComponentNetMessage.Equipped:
                    EquippedBy((int) message.MessageParameters[1], (EquipmentSlot) message.MessageParameters[2]);
                    break;
                case EquippableComponentNetMessage.UnEquipped:
                    UnEquipped();
                    break;
            }
        }

        private void EquippedBy(int uid, EquipmentSlot wearloc)
        {
            Owner.SendMessage(this, ComponentMessageType.ItemEquipped);
            Owner.AddComponent(ComponentFamily.Mover,
                               Owner.EntityManager.ComponentFactory.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, uid);
            switch (wearloc)
            {
                case EquipmentSlot.Back:
                    SendDrawDepth(DrawDepth.MobOverAccessoryLayer);
                    break;
                case EquipmentSlot.Belt:
                    SendDrawDepth(DrawDepth.MobUnderAccessoryLayer);
                    break;
                case EquipmentSlot.Ears:
                    SendDrawDepth(DrawDepth.MobUnderAccessoryLayer);
                    break;
                case EquipmentSlot.Eyes:
                    SendDrawDepth(DrawDepth.MobUnderAccessoryLayer);
                    break;
                case EquipmentSlot.Feet:
                    SendDrawDepth(DrawDepth.MobUnderClothingLayer);
                    break;
                case EquipmentSlot.Hands:
                    SendDrawDepth(DrawDepth.MobOverAccessoryLayer);
                    break;
                case EquipmentSlot.Head:
                    SendDrawDepth(DrawDepth.MobOverClothingLayer);
                    break;
                case EquipmentSlot.Inner:
                    SendDrawDepth(DrawDepth.MobUnderClothingLayer);
                    break;
                case EquipmentSlot.Mask:
                    SendDrawDepth(DrawDepth.MobUnderAccessoryLayer);
                    break;
                case EquipmentSlot.Outer:
                    SendDrawDepth(DrawDepth.MobOverClothingLayer);
                    break;
            }
        }

        private void SendDrawDepth(DrawDepth dd)
        {
            Owner.SendMessage(this, ComponentMessageType.SetWornDrawDepth, dd);
        }

        private void UnEquipped()
        {
            Owner.SendMessage(this, ComponentMessageType.ItemUnEquipped);
            Owner.AddComponent(ComponentFamily.Mover,
                               Owner.EntityManager.ComponentFactory.GetComponent("NetworkMoverComponent"));
        }
    }
}