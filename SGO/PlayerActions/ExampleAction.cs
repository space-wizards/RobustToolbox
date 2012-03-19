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
using System.Drawing;

namespace SGO
{
    public class ExampleAction : PlayerAction
    {
        public ExampleAction(uint _uid, PlayerActionComp _parent)
            : base(_uid, _parent)
        {
        }

        public override void OnUse(Entity targetEnt)
        {
            if (targetEnt.HasComponent(ComponentFamily.StatusEffects)) //Use component messages instead.
            {
                StatusEffectComp statComp = (StatusEffectComp)targetEnt.GetComponent(ComponentFamily.StatusEffects);
                statComp.AddEffect("Bleeding", 10);
                parent.StartCooldown(this);
            }
        }
    }
}
