using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Atom.Mob
{
    public class Mob : Atom
    {
        
        // TODO Make these some sort of well-organized global constant
        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public Atom leftHandItem; // Just a temporary storage spot, for now.
        public Atom rightHandItem;
        public MobHand selectedHand = MobHand.LHand; // The hand we are currently using, and also used to tell clients which
        // hand to attach items to on remote mobs when the pick something up.

        public Mob()
            : base()
        {
            meshName = "male.mesh";
        }

    }
}
