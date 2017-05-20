using SS14.Shared;

namespace SS14.Client.GameObjects
{
    public class HumanEquipmentComponent : EquipmentComponent
    {
        public HumanEquipmentComponent()
        {
            //These shit lines allow the fucking shit to be added to the shit
            ActiveSlots.Add(EquipmentSlot.Back);
            ActiveSlots.Add(EquipmentSlot.Belt);
            ActiveSlots.Add(EquipmentSlot.Ears);
            ActiveSlots.Add(EquipmentSlot.Eyes);
            ActiveSlots.Add(EquipmentSlot.Feet);
            ActiveSlots.Add(EquipmentSlot.Hands);
            ActiveSlots.Add(EquipmentSlot.Head);
            ActiveSlots.Add(EquipmentSlot.Inner);
            ActiveSlots.Add(EquipmentSlot.Mask);
            ActiveSlots.Add(EquipmentSlot.Outer);
        }
    }
}