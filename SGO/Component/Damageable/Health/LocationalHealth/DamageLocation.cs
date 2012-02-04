using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using SS13_Shared;
using ServerServices;
using ServerInterfaces;

namespace SGO
{
    public class DamageLocation
    {
        public BodyPart location;
        public int maxHealth;
        public int currentHealth;

        public Dictionary<DamageType, int> damageIndex = new Dictionary<DamageType, int>();

        public DamageLocation(BodyPart myPart, int maxHealth)
        {
            location = myPart;
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
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

            foreach (KeyValuePair<DamageType, int> curr in damageIndex)
                updatedHealth -= curr.Value;

            currentHealth = updatedHealth;

            return currentHealth;
        }
    }
}
