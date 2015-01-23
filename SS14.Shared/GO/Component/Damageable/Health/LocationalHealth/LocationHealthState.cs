using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace SS14.Shared.GO.Component.Damageable.Health.LocationalHealth
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