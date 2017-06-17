using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Damageable.Health;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    public class HealthComponent : DamageableComponent
    {
        public override string Name => "Health";
        // TODO use state system
        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected override void ApplyDamage(IEntity damager, int damageamount, DamageType damType)
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
