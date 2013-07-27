using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.StatusEffect;
using SS13_Shared.GO.StatusEffect;

namespace SGO
{
    public class StatusEffectComp : GameObjectComponent
    {
        public List<StatusEffect> Effects = new List<StatusEffect>();
        private uint uidCurr;

        public StatusEffectComp()
        {
            Family = ComponentFamily.StatusEffects;
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
            newEffect.typeName = typeName;
            newEffect.OnAdd();
        }

        public void RemoveEffect(uint uid)
        {
            StatusEffect toRemove = Effects.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
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

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "AddEffect":
                    AddEffect(parameter.GetValue<string>(),10);
                    break;
                default:
                    base.SetParameter(parameter);
                    break;
            }
        } 

        public override List<ComponentParameter> GetParameters()
        {
            var cparams = base.GetParameters();
            cparams.Add(new ComponentParameter("AddEffect", ""));
            return cparams;
        }

        public override ComponentState GetComponentState()
        {
            var states = new List<StatusEffectState>();
            foreach(var effect in Effects)
            {
                states.Add(effect.GetState());
            }
            return new StatusEffectComponentState(states);
        }
    }
}