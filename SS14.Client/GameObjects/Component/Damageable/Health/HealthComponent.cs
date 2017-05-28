using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Damageable.Health;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Behaves like the damageable component but tracks health as well
    /// </summary>
    [IoCTarget]
    public class HealthComponent : DamageableComponent
    {
        public override string Name => "Health";
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
