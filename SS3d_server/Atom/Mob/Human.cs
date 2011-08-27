using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Mob
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
