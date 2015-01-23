using SS14.Shared;
using SS14.Shared.GO;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class DamageLocation
    {
        public int CurrentHealth;

        public Dictionary<DamageType, int> DamageIndex = new Dictionary<DamageType, int>();
        public BodyPart Location;
        public int MaxHealth;

        public DamageLocation(BodyPart myPart, int maxHealth, int currHealth)
        {
            Location = myPart;
            MaxHealth = maxHealth;
            CurrentHealth = currHealth;
        }

        public int UpdateTotalHealth()
        {
            int updatedHealth = DamageIndex.Aggregate(MaxHealth, (current, curr) => current - curr.Value);

            CurrentHealth = updatedHealth;

            return CurrentHealth;
        }
    }
}