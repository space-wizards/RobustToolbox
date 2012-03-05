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
using System.Diagnostics;

namespace SGO
{
    public class Bleeding : StatusEffect
    {
        Stopwatch bleedDmgTimer = new Stopwatch();
        Stopwatch bleedEntTimer = new Stopwatch();

        public Bleeding(uint _uid, Entity _affected, uint duration = 0)
            : base(_uid, _affected, duration)
        {
            isDebuff = true;
            isUnique = true;
            family = StatusEffectFamily.Damage;
        }

        public override void OnAdd()
        {
            bleedDmgTimer.Restart();
            bleedEntTimer.Restart();
        }

        public override void OnRemove()
        {
            bleedDmgTimer.Stop();
            bleedEntTimer.Stop();
        }

        public override void OnUpdate()
        {
            if (bleedDmgTimer.ElapsedMilliseconds >= 300)
            {
                bleedDmgTimer.Restart();
                affected.SendMessage(this, ComponentMessageType.Damage, null, affected, 1, DamageType.Slashing, BodyPart.Torso);
            }

            if (bleedEntTimer.ElapsedMilliseconds >= 1500)
            {
                bleedEntTimer.Restart();
                EntityManager.Singleton.SpawnEntity("Blood", affected.position);
            }
        }
    }
}