using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Item.Organs.External
{
    public class Head : External
    {
        public Head()
            : base()
        {
            name = "Head";
        }

        public override void SetUp(Mob.Mob _owner)
        {
            max_blood = 100;
            normalChildNumber = 1;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }

        public override void HeartBeat()
        {
            return;
        }
    }
}
