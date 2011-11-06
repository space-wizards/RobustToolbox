using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Health.Organs.External
{
    public class LArm : External
    {
        public LArm()
            : base()
        {
            name = "Left Arm";
        }

        public override void SetUp(HealthComponent _owner)
        {
            max_blood = 9;
            normalChildNumber = 0;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
