using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organs = SGO.Component.Health.Organs;

namespace SGO
{
    public class HumanHealthComponent : HealthComponent
    {
        public HumanHealthComponent()
        {
            
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            organs.Add(new Organs.External.Head());
            organs.Add(new Organs.External.Torso());
            organs.Add(new Organs.External.LArm());
            organs.Add(new Organs.External.RArm());
            organs.Add(new Organs.External.Groin());
            organs.Add(new Organs.External.LLeg());
            organs.Add(new Organs.External.RLeg());
            organs.Add(new Organs.Internal.Heart());
            foreach (Organs.Organ organ in organs)
            {
                organ.SetUp(this);
            }
        }

    }
}
