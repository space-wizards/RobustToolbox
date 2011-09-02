using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Object.Wall
{
    [Serializable()]
    class Glass : Object
    {
        public Glass()
            : base()
        {
            name = "glass";
            damageable = true;
        }

        public override void Damage(int amount)
        {
            base.Damage(amount);
            if ((float)currentHealth / (float)maxHealth <= 0)
                SetSpriteState(1);

        }
    }
}
