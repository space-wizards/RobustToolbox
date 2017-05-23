using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Damageable.Health;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    [Component("Health")]
    public class HealthComponent : DamageableComponent
    {
        // TODO use state system
        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected override void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            base.ApplyDamage(damager, damageamount, damType);
            SendHealthUpdate();
        }

        protected override void ApplyDamage(int p)
        {
            base.ApplyDamage(p);
            SendHealthUpdate();
        }

        public override ComponentState GetComponentState()
        {
            return new HealthComponentState(isDead, GetHealth(), maxHealth);
        }
    }
}
