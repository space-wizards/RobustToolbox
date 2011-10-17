using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Health.Organs.External
{
    public class RLeg : External
    {
        
        public RLeg()
            : base()
        {
            name = "Right leg";
        }

        public override void SetUp(HealthComponent _owner)
        {
            max_blood = 60;
            normalChildNumber = 0;
            masterConnectionType = typeof(Groin);
            base.SetUp(_owner);
        }
    }
}
