using System;
using System.Collections.Generic;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class ParticleSystemComponentState : ComponentState
    {
        private readonly Dictionary<string, bool> _emitters;

        public IReadOnlyDictionary<string, bool> Emitters => _emitters;

        public ParticleSystemComponentState(Dictionary<string, bool> emitters)
            : base(NetIDs.PARTICLE_SYSTEM)
        {
            _emitters = emitters;
        }
    }
}
