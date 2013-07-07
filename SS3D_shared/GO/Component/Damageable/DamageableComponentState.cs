using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Component.Damageable
{
    [Serializable]
    public class DamageableComponentState : ComponentState
    {
        public bool IsDead;
        public DamageableComponentState(bool isDead) 
            :base(ComponentFamily.Damageable)
        {
            IsDead = isDead;
        }
    }
}
