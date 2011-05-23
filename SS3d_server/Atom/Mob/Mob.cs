using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Mob
{
    public class Mob : Atom
    {
        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public float speed = 0.0f;

        public Atom leftHandItem; // Just a temporary storage spot, for now.
        public Atom rightHandItem;
        public MobHand selectedHand = MobHand.LHand; // The hand we are currently using, and also used to tell clients which
        // hand to attach items to on remote mobs when the pick something up.

        public Mob()
            : base()
        {

        }
    }
}
