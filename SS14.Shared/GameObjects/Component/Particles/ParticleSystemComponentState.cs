using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.Components.Particles
{
    [Serializable]
    public class ParticleSystemComponentState : ComponentState
    {
        public Dictionary<string, Boolean> emitters;

        public ParticleSystemComponentState(Dictionary<string, Boolean> _emitters)
            : base(ComponentFamily.Particles)
        {
            emitters = _emitters;
        }
    }
}
