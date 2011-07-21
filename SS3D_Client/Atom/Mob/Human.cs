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
            spritename = "Human";
            /*meshName = "male_new.mesh";
            scale = new Mogre.Vector3(1f, 1f, 1f);
            offset = new Mogre.Vector3(0, 0, 0);*/
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
