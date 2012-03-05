using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;
using System.Linq;
using System.Text;
using System.Reflection;
using System;

namespace SGO
{
    public class StatusEffectComp : GameObjectComponent
    {
        private uint uidCurr = 0;

        public List<StatusEffect> Effects = new List<StatusEffect>();

        public StatusEffectComp()
            : base()
        {
            family = SS13_Shared.GO.ComponentFamily.StatusEffects;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            base.HandleNetworkMessage(message, client);
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (StatusEffect effect in Effects.ToArray())
            {
                effect.OnUpdate();
                if (effect.doesExpire && DateTime.Compare(DateTime.Now, effect.expiresAt) > 0)
                    RemoveEffect(effect.uid);
            }
        }

        public void AddEffect(string typeName)
        {
            Type t = Type.GetType("SGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof(StatusEffect))) return;

            uint nextUid = uidCurr++; //Increases uid even if adding fails due to effect being unique. fix.

            StatusEffect newEffect = (StatusEffect)Activator.CreateInstance(t, new object[] { nextUid, this.Owner });

            if (newEffect.isUnique && HasEffect(typeName)) return;

            Effects.Add(newEffect);
            newEffect.OnAdd();

            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.AddStatusEffect, typeName, nextUid, newEffect.doesExpire, newEffect.doesExpire ? newEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds : 0, (int)newEffect.family);
        }

        public void RemoveEffect(uint uid)
        {
            StatusEffect toRemove = Effects.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.RemoveStatusEffect, toRemove.uid);
            }
        }

        public void RemoveEffect(string typeName)
        {
            StatusEffect toRemove = Effects.FirstOrDefault(x => x.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.RemoveStatusEffect, toRemove.uid);
            }
        }

        public bool HasEffect(string typeName)
        {
            foreach (StatusEffect effect in Effects) 
                if (effect.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) 
                    return true;

            return false;
        }

        public bool HasFamily(StatusEffectFamily family)
        {
            foreach (StatusEffect effect in Effects)
                if (effect.family == family)
                    return true;
            return false;
        }
    }
}
