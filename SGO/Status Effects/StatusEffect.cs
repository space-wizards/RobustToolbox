using System;
using SS13_Shared.GO;

namespace SGO
{
    public class StatusEffect
    {
        public readonly uint uid;
        protected Entity affected;
        public Boolean doesExpire = true;
        public DateTime expiresAt;

        public StatusEffectFamily family = StatusEffectFamily.None;
        public Boolean isDebuff = true;
        public Boolean isUnique; //May not have more than one instance of this effect?

        public StatusEffect(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            //Do not add more parameters to the constructors or bad things happen.
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