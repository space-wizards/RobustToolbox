using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO.Component.Damageable.Health.LocationalHealth
{
    [Serializable]
    public class LocationHealthState : INetSerializableType
    {
        public BodyPart Location;
        public int MaxHealth;
        public int CurrentHealth;
        public Dictionary<DamageType, int> DamageIndex = new Dictionary<DamageType, int>();  
    }
}
