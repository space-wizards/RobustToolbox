using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
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
