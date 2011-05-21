using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Mogre;

namespace SS3D_shared
{
    public abstract class Mob : AtomBaseClass
    {
        public ushort mobID = 0;
        public List<InterpolationPacket> interpolationPacket;
        public ServerItemInfo serverInfo;
        public AnimationState animState;

        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public float speed = 0.0f;

        public Item leftHandItem; // Just a temporary storage spot, for now.
        public Item rightHandItem;
        public MobHand selectedHand = MobHand.LHand; // The hand we are currently using, and also used to tell clients which
                                                     // hand to attach items to on remote mobs when the pick something up.
        
        public BillboardSet billboardSet; // Shows a chat bubble over their head when they speak.
        public bool speaking = false; // Are we talking right now?
        public float speakTime = 0; // The amount of time we've been speaking for.


        public Mob()
        {
            AtomType = global::AtomType.Mob;
        }

    }
}
