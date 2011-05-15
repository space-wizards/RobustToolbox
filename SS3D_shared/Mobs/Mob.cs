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
    }
}
