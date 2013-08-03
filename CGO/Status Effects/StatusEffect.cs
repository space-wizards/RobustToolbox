using System;
using GameObject;
using SS13_Shared.GO;

namespace CGO
{
    public class StatusEffect
    {
        public DateTime expiresAt;        //Don't set this clientside. The component handles this.
        public Boolean doesExpire = true; //Don't set this clientside. The component handles this.

        public Boolean isDebuff = true;

        public readonly uint uid = 0;      //Don't set this clientside. The component handles this.

        protected Entity affected;
        public StatusEffectFamily family = StatusEffectFamily.None;

        public String name = "Effect";
        public String description = "A Status Effect.";
        public String icon;

        public StatusEffect(uint _uid, Entity _affected) //Do not add more parameters to the constructors or bad things happen.
        {
            uid = _uid;
            affected = _affected;
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

        internal void UpdateEffectState(SS13_Shared.GO.StatusEffect.StatusEffectState effectState)
        {
            expiresAt = effectState.ExpiresAt;
            doesExpire = effectState.DoesExpire;
        }
    }
}
