using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Health.Organs.External
{
    public class Head : External
    {
        public Head()
            : base()
        {
            name = "Head";
        }

        public override void SetUp(HealthComponent _owner)
        {
            max_blood = 20;
            normalChildNumber = 1;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
