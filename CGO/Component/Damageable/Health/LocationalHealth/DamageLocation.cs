using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;
using SS13_Shared;

namespace CGO
{
    public class DamageLocation
    {
        public BodyPart Location;
        public int MaxHealth;
        public int CurrentHealth;

        public Dictionary<DamageType, int> DamageIndex = new Dictionary<DamageType, int>();

        public DamageLocation(BodyPart myPart, int maxHealth, int currHealth)
        {
            Location = myPart;
            MaxHealth = maxHealth;
            CurrentHealth = currHealth;
        }

        public int UpdateTotalHealth()
        {
            var updatedHealth = DamageIndex.Aggregate(MaxHealth, (current, curr) => current - curr.Value);

            CurrentHealth = updatedHealth;

            return CurrentHealth;
        }
    }
}
