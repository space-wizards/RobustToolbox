using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using System.Collections;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class StatusEffect
    {
        public DateTime expiresAt;
        public Boolean doesExpire = true;

        public Boolean isDebuff = true;
        public Boolean isUnique = false; //May not have more than one instance of this effect?

        public readonly uint uid = 0;
        protected Entity affected;

        public StatusEffectFamily family = StatusEffectFamily.None;

        public StatusEffect(uint _uid, Entity _affected, uint duration = 0) //Do not add more parameters to the constructors or bad things happen.
        {
            uid = _uid;
            affected = _affected;

            if (duration > 0)
            {
                expiresAt = DateTime.Now.AddSeconds(duration);
                doesExpire = true;
            }
            else
            {
                expiresAt = DateTime.Now;
                doesExpire = false;
            }
        }

        public virtual void OnAdd()
        {
        }

        public virtual void OnRemove()
        {
        }

        public virtual void OnUpdate()
        {

        }
    }
}
