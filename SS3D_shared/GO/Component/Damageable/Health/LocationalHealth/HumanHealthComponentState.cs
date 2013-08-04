using System;
using System.Collections.Generic;

namespace SS13_Shared.GO.Component.Damageable.Health.LocationalHealth
{
    [Serializable]
    public class HumanHealthComponentState : HealthComponentState
    {
        public List<LocationHealthState> LocationHealthStates;

        public HumanHealthComponentState(bool isDead, float health, float maxHealth,
                                         List<LocationHealthState> locationHealthStates)
            : base(isDead, health, maxHealth)
        {
            LocationHealthStates = locationHealthStates;
        }
    }
}