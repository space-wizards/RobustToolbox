using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;

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
    }
}
