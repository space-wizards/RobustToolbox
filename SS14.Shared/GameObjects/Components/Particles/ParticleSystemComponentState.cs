using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class ParticleSystemComponentState : ComponentState
    {
        public readonly Dictionary<string, bool> emitters;

        public ParticleSystemComponentState(Dictionary<string, bool> _emitters)
            : base(NetIDs.PARTICLE_SYSTEM)
        {
            emitters = _emitters;
        }
    }
}
