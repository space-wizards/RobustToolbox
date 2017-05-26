using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class HumanEquipmentComponent : EquipmentComponent
    {
        public override string Name => "HumanEquipment";
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
