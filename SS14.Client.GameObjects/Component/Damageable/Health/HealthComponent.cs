using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Damageable.Health;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Behaves like the damageable component but tracks health as well
    /// </summary>
    public class HealthComponent : DamageableComponent
    {
        //Useful for objects that need to show different stages of damage clientside.
        protected float Health;
        protected float MaxHealth;

        public override Type StateType
        {
            get { return typeof (HealthComponentState); }
        }

        public virtual float GetMaxHealth()
        {
            return MaxHealth;
        }

        public virtual float GetHealth()
        {
            return Health;
        }

        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((HealthComponentState) state);

            Health = state.Health;
            MaxHealth = state.MaxHealth;
        }
    }
}