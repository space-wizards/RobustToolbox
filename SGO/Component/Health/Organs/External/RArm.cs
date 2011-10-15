using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Health.Organs.External
{
    public class RArm : External
    {
        public RArm()
            : base()
        {
            name = "Right Arm";
        }

        public override void SetUp(HealthComponent _owner)
        {
            max_blood = 60;
            normalChildNumber = 0;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
