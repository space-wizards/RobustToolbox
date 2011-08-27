using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Item.Organs.External
{
    public class RArm : External
    {
        public RArm()
            : base()
        {
            name = "Right Arm";
        }

        public override void SetUp(Mob.Mob _owner)
        {
            max_blood = 60;
            normalChildNumber = 0;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
