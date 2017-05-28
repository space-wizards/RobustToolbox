using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    public class HumanEquipmentComponent : EquipmentComponent
    {
        public override string Name => "HumanEquipment";
        public HumanEquipmentComponent()
        {
            //These shit lines allow the fucking shit to be added to the shit
            activeSlots.Add(EquipmentSlot.Back);
            activeSlots.Add(EquipmentSlot.Belt);
            activeSlots.Add(EquipmentSlot.Ears);
            activeSlots.Add(EquipmentSlot.Eyes);
            activeSlots.Add(EquipmentSlot.Feet);
            activeSlots.Add(EquipmentSlot.Hands);
            activeSlots.Add(EquipmentSlot.Head);
            activeSlots.Add(EquipmentSlot.Inner);
            activeSlots.Add(EquipmentSlot.Mask);
            activeSlots.Add(EquipmentSlot.Outer);
        }
    }
}
