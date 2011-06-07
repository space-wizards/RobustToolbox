using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Item.Container
{
    public class Toolbox : Item
    {
        public Toolbox()
            : base()
        {
            meshName = "toolbox.mesh";
            name = "Toolbox";
            heldQuat = new Mogre.Quaternion(new Mogre.Degree(90), new Mogre.Vector3(1,0,1));
            heldOffset = new Mogre.Vector3(5f, 0f, 0.6f);

        }
    }
}
