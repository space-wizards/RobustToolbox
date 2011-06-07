using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Mob
{
    public class Human : Mob
    {
        public Human()
            : base()
        {
            meshName = "male_new.mesh";
            scale = new Mogre.Vector3(.6f, .6f, .6f);
            offset = new Mogre.Vector3(0, 14, 0);
        }

        public override void MoveForward()
        {
            base.MoveForward();
        }

        public override void MoveBack()
        {
            base.MoveBack();
        }
    }
}
