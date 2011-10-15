using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class HumanEquipmentComponent : EquipmentComponent
    {
        public HumanEquipmentComponent()
            : base()
        {
            //These shit lines allow the fucking shit to be added to the shit
            activeSlots.Add(GUIBodyPart.Back);
            activeSlots.Add(GUIBodyPart.Belt);
            activeSlots.Add(GUIBodyPart.Ears);
            activeSlots.Add(GUIBodyPart.Eyes);
            activeSlots.Add(GUIBodyPart.Feet);
            activeSlots.Add(GUIBodyPart.Hands);
            activeSlots.Add(GUIBodyPart.Head);
            activeSlots.Add(GUIBodyPart.Inner);
            activeSlots.Add(GUIBodyPart.Mask);
            activeSlots.Add(GUIBodyPart.Outer);
        }
    }
}
