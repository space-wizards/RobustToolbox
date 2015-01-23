using System;
using System.Collections.Generic;

namespace SS14.Shared.GO.Component.EntityStats
{
    [Serializable]
    public class EntityStatsComponentState : ComponentState
    {
        public Dictionary<DamageType, int> ArmorStats = new Dictionary<DamageType, int>();

        public EntityStatsComponentState(Dictionary<DamageType, int> armorStats)
            :base(ComponentFamily.EntityStats)
        {
            ArmorStats = armorStats;
        }
    }
}