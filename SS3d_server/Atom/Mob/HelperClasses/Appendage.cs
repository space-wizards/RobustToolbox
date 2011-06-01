using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Mob.HelperClasses
{
    public class Appendage
    {
        public string appendageName;
        public Item.Item heldItem = null;
        public Mob owner;

        public Appendage(string _appendageName, Mob _owner)
        {
            appendageName = _appendageName;
            owner = _owner;
        }
    }
}
