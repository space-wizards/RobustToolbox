using SS14.Shared;
using SS14.Shared.GO;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Server.GameObjects
{
    public class DamageLocation
    {
        public int currentHealth;

        public Dictionary<DamageType, int> damageIndex = new Dictionary<DamageType, int>();
        public BodyPart location;
        public int maxHealth;

        public DamageLocation(BodyPart tzone, int maxHealth)
        {
            location = tzone;
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
        }

        public void AddDamage(DamageType type, int amount)
        {
            if (damageIndex.Keys.Contains(type))
                damageIndex[type] += amount;
            else
                damageIndex.Add(type, amount);

            UpdateTotalHealth();
        }

        public void HealDamage(DamageType type, int amount)
        {
            if (damageIndex.Keys.Contains(type))
            {
                damageIndex[type] -= amount;
                if (damageIndex[type] <= 0)
                    damageIndex.Remove(type);
            }

            UpdateTotalHealth();
        }

        public int UpdateTotalHealth()
        {
            int updatedHealth = maxHealth;

            foreach (var curr in damageIndex)
                updatedHealth -= curr.Value;

            currentHealth = updatedHealth;

            return currentHealth;
        }
    }
}