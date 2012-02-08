using System.Collections.Generic;
using SS13_Shared.GO;
using SS13_Shared;

namespace CGO.Component.Damageable.Health.LocationalHealth
{
    public class DamageLocation
    {
        public BodyPart location;
        public int maxHealth;
        public int currentHealth;

        public Dictionary<DamageType, int> damageIndex = new Dictionary<DamageType, int>();

        public DamageLocation(BodyPart myPart, int maxHealth, int currHealth)
        {
            location = myPart;
            this.maxHealth = maxHealth;
            this.currentHealth = currHealth;
        }

        public int UpdateTotalHealth()
        {
            int updatedHealth = maxHealth;

            foreach (KeyValuePair<DamageType, int> curr in damageIndex)
                updatedHealth -= curr.Value;

            currentHealth = updatedHealth;

            return currentHealth;
        }
    }
}
