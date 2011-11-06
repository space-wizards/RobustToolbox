using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Health.Organs.External
{
    public class Groin : External
    {
        public Groin()
            : base()
        {
            name = "Groin";
        }

        public override void SetUp(HealthComponent _owner)
        {
            max_blood = 14;
            normalChildNumber = 2;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
