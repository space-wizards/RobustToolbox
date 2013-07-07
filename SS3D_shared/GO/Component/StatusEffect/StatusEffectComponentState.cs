using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO.StatusEffect;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.Component.StatusEffect
{
    [Serializable]
    public class StatusEffectComponentState : ComponentState
    {
        public List<StatusEffectState> EffectStates;
        public StatusEffectComponentState(List<StatusEffectState> effectStates)
            :base(ComponentFamily.StatusEffects)
        {
            EffectStates = effectStates;
        }
    }
}
