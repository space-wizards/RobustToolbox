using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

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

        //Current animation state -- or at least the one we want to add some time to. This will need to become more robust.
        public AnimationState animState;

        public Mob()
            : base()
        {
            meshName = "male.mesh";

        }

        public override void SetUp(ushort _uid, AtomManager _atomManager)
        {
            base.SetUp(_uid, _atomManager);

            animState = Entity.GetAnimationState("idle");
            animState.Loop = true;
            animState.Enabled = true;
        }

        public override void Update()
        {
            base.Update();

            // Update Animation. Right now, anything animated will have to be updated in entirety every tick.
            TimeSpan t = atomManager.gameState.lastUpdate - atomManager.gameState.now;
            animState.AddTime((float)t.TotalMilliseconds / 1000f);

            updateRequired = true;
        }
    }
}
