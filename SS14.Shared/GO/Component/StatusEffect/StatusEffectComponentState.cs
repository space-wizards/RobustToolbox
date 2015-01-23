using SS14.Shared.GO.StatusEffect;
using System;
using System.Collections.Generic;

namespace SS14.Shared.GO.Component.StatusEffect
{
    [Serializable]
    public class StatusEffectComponentState : ComponentState
    {
        public List<StatusEffectState> EffectStates;

        public StatusEffectComponentState(List<StatusEffectState> effectStates)
            : base(ComponentFamily.StatusEffects)
        {
            EffectStates = effectStates;
        }
    }
}