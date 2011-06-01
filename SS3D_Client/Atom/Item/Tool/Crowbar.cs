using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Item.Tool
{
    public class Crowbar : Item
    {

        public Crowbar()
            : base()
        {
            meshName = "crowbar.mesh";
            name = "Crowbar";
            heldQuat = new Mogre.Quaternion(new Mogre.Degree(90), Mogre.Vector3.UNIT_Y);
            heldOffset = new Mogre.Vector3(3, 0, 0);

        }
    }
}
