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
            meshName = "male.mesh";

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
