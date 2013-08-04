using System;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.StatusEffect;

namespace CGO
{
    public class StatusEffect
    {
        public readonly uint uid = 0; //Don't set this clientside. The component handles this.

        protected Entity affected;
        public String description = "A Status Effect.";
        public Boolean doesExpire = true; //Don't set this clientside. The component handles this.
        public DateTime expiresAt; //Don't set this clientside. The component handles this.
        public StatusEffectFamily family = StatusEffectFamily.None;
        public String icon;
        public Boolean isDebuff = true;
        public String name = "Effect";

        public StatusEffect(uint _uid, Entity _affected)
            //Do not add more parameters to the constructors or bad things happen.
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

        internal void UpdateEffectState(StatusEffectState effectState)
        {
            expiresAt = effectState.ExpiresAt;
            doesExpire = effectState.DoesExpire;
        }
    }
}