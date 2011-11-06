using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class DamageableComponent : GameObjectComponent
    {
        private float maxHealth = 100;
        private float currentHealth = 100;
        public DamageableComponent()
            : base()
        {
            family = SS3D_shared.GO.ComponentFamily.Damageable;
        }

        public virtual float GetHealth()
        {
            return currentHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }
    }
}
