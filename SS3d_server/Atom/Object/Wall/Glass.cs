using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

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
            collidable = true;
        }

        public override void Damage(int amount, int damagerId)
        {
            base.Damage(amount, damagerId);
            if ((float)currentHealth / (float)maxHealth <= 0)
            {
                SetSpriteState(1);
                collidable = false;
                SendCollidable();
            }

        }

        public Glass(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
            damageable = true;
            collidable = true;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
