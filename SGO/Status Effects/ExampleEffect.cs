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
    public class ExampleEffect : StatusEffect
    {
        public ExampleEffect(uint _uid, Entity _affected)  //Do not add more parameters to the constructors or bad things happen.
            : base(_uid, _affected)
        {
            expiresAt = DateTime.Now.AddSeconds(10);
            doesExpire = true;
            isDebuff = false;
            isUnique = false;
            family = StatusEffectFamily.None;
        }

        public override void OnAdd()
        {
        }

        public override void OnRemove()
        {
        }

        public override void OnUpdate()
        {
        }
    }
}