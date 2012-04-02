using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class StatusEffectComp : GameObjectComponent
    {
        public List<StatusEffect> Effects = new List<StatusEffect>();
        private uint uidCurr;

        public StatusEffectComp()
        {
            family = ComponentFamily.StatusEffects;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            base.HandleNetworkMessage(message, client);
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

        public void AddEffect(string typeName, uint duration = 0, params object[] arguments)
        {
            Type t = Type.GetType("SGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof (StatusEffect))) return;

            uint nextUid = uidCurr++; //Increases uid even if adding fails due to effect being unique. fix.

            var newEffect = (StatusEffect) Activator.CreateInstance(t, new object[] {nextUid, Owner, duration});

            if (newEffect.isUnique && HasEffect(typeName))
            {
                //if (newEffect.doesExpire) //Its unique but has a duration. Refresh duration. TODO: Update client.
                //{
                //    StatusEffect oldEffect = Effects.FirstOrDefault(x => x.GetType() == t);
                //    if (oldEffect != null) oldEffect.expiresAt = newEffect.expiresAt;
                //}
                return;
            }

            Effects.Add(newEffect);
            newEffect.OnAdd();

            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                              ComponentMessageType.AddStatusEffect, typeName, nextUid,
                                              newEffect.doesExpire,
                                              newEffect.doesExpire
                                                  ? newEffect.expiresAt.Subtract(DateTime.Now).TotalSeconds
                                                  : 0, (int) newEffect.family);
        }

        public void RemoveEffect(uint uid)
        {
            StatusEffect toRemove = Effects.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                  ComponentMessageType.RemoveStatusEffect, toRemove.uid);
            }
        }

        public void RemoveEffect(string typeName)
        {
            StatusEffect toRemove =
                Effects.FirstOrDefault(
                    x => x.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                  ComponentMessageType.RemoveStatusEffect, toRemove.uid);
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