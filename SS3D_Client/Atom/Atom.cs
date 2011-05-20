using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
using SS3D_shared.HelperClasses;

namespace SS3D.Atom
{
    public class Atom // CLIENT SIDE
    {
        // GRAPHICS
        public SceneNode Node;
        public Entity Entity;
        public string meshName = "ogrehead.mesh";

        public string name;
        public ushort UID
        {
            private set;
            get;
        }

        public List<InterpolationPacket> interpolationPacket;
    
    }
}
