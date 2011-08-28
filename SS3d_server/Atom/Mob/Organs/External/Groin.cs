using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Item.Organs.External
{
    public class Groin : External
    {
        public Groin()
            : base()
        {
            name = "Groin";
        }

        public override void SetUp(Mob.Mob _owner)
        {
            max_blood = 80;
            normalChildNumber = 2;
            masterConnectionType = typeof(Torso);
            base.SetUp(_owner);
        }
    }
}
