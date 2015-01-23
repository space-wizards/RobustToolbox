using System;

namespace SS14.Shared.GO.Component.Damageable
{
    [Serializable]
    public class DamageableComponentState : ComponentState
    {
        public bool IsDead;

        public DamageableComponentState(bool isDead)
            : base(ComponentFamily.Damageable)
        {
            IsDead = isDead;
        }
    }
}