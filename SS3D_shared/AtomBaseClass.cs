using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    public class AtomBaseClass //This class is the base class for all other objects in the game. 
                               //Think of it as byonds ATOM. Add shared variables here.
                               //This should always be in the entities UserObject. So make sure to set it.
    {

        public AtomType AtomType = AtomType.None;
        public SceneNode Node;
        public Entity Entity;
        public string name;
    }
}
