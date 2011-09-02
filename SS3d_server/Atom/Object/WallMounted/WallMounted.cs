using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Object.WallMounted
{
    [Serializable()]
    class WallMounted : Object
    {
        public WallMounted()
            : base()
        {
            name = "wallmountedobj";
        }
    }
}
