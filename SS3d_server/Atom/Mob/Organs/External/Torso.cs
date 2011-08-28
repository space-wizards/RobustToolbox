using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Item.Organs.External
{
    public class Torso : External
    {
        public Torso()
            : base()
        {
            name = "Torso";
        }

        public override void SetUp(Mob.Mob _owner)
        {
            max_blood = 200;
            normalChildNumber = 4;
            masterConnectionType = typeof(Head);
            base.SetUp(_owner);
        }
    }
}
