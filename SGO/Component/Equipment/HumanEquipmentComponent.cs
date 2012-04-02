using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace SGO
{
    public class HumanEquipmentComponent : EquipmentComponent
    {
        public HumanEquipmentComponent()
            : base()
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
