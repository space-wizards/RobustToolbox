using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects.Components.EntityStats
{
    [Serializable]
    public class EntityStatsComponentState : ComponentState
    {
        public Dictionary<DamageType, int> ArmorStats;

        public EntityStatsComponentState(Dictionary<DamageType, int> armorStats)
            :base(ComponentFamily.EntityStats)
        {
            ArmorStats = armorStats;
        }
    }
}
