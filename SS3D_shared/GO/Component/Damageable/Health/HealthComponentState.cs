using System;

namespace SS13_Shared.GO.Component.Damageable.Health
{
    [Serializable]
    public class HealthComponentState : DamageableComponentState
    {
        public float Health;
        public float MaxHealth;

        public HealthComponentState(bool isDead, float health, float maxHealth)
            : base(isDead)
        {
            Health = health;
            MaxHealth = maxHealth;
        }
    }
}