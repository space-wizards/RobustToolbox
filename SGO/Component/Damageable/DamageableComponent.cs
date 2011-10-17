using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO.Component.Damageable
{
    public class DamageableComponent : GameObjectComponent
    {
        public DamageableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Damageable;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case SS3D_shared.GO.ComponentMessageType.Damage:
                    /// Who damaged, how much, what type
                    ApplyDamage((Entity)list[0], (int)list[0], (DamageType)list[1]);
                    break;

            }
        }

        private void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            throw new NotImplementedException();
        }

        private void ApplyDamage(int p)
        {
            throw new NotImplementedException();
        }

    }
}
