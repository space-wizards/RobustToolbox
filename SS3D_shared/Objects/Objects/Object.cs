using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    class Object : AtomBaseClass
    {
        public ObjectType ObjectType = ObjectType.None;

        public Object()
        {
            AtomType = global::AtomType.Object;
        }

    }
}
