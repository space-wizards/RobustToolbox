using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Mob
{
    public class Human : Mob
    {
        public Human()
            : base()
        {

        }

        protected override void initAppendages()
        {
            
            organs.Add(new Item.Organs.External.Head());
            organs.Add(new Item.Organs.External.Torso());
            organs.Add(new Item.Organs.External.LArm());
            organs.Add(new Item.Organs.External.RArm());
            organs.Add(new Item.Organs.External.Groin());
            organs.Add(new Item.Organs.External.LLeg());
            organs.Add(new Item.Organs.External.RLeg());
            organs.Add(new Item.Organs.Internal.Heart());
            foreach (Item.Organs.Organ organ in organs)
            {
                organ.SetUp(this);
            }

            if (equippedAtoms == null)
                equippedAtoms = new Dictionary<GUIBodyPart, Item.Item>();

            equippedAtoms.Add(GUIBodyPart.Ears, null);
            equippedAtoms.Add(GUIBodyPart.Eyes, null);
            equippedAtoms.Add(GUIBodyPart.Head, null);
            equippedAtoms.Add(GUIBodyPart.Mask, null);
            equippedAtoms.Add(GUIBodyPart.Inner, null);
            equippedAtoms.Add(GUIBodyPart.Outer, null);
            equippedAtoms.Add(GUIBodyPart.Hands, null);
            equippedAtoms.Add(GUIBodyPart.Feet, null);
            equippedAtoms.Add(GUIBodyPart.Belt, null);
            equippedAtoms.Add(GUIBodyPart.Back, null);

            base.initAppendages();
        }

        public override void Update(float framePeriod)
        {
            foreach(Item.Organs.Organ organ in organs)
            {
                organ.Process(framePeriod);
            }
            base.Update(framePeriod);
            updateRequired = true;

        }
    }
}
