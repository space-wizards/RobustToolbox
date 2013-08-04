using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.Component.Damageable.Health.LocationalHealth
{
    [Serializable]
    public class LocationHealthState : INetSerializableType
    {
        public int CurrentHealth;
        public Dictionary<DamageType, int> DamageIndex = new Dictionary<DamageType, int>();
        public BodyPart Location;
        public int MaxHealth;
    }
}