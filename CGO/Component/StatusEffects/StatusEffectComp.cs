using System;
using System.Collections.Generic;
using System.Linq;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.StatusEffect;
using SS13_Shared.GO.StatusEffect;

namespace CGO
{
    public class StatusEffectComp : Component
    {
        #region Delegates

        public delegate void StatusEffectsChangedHandler(StatusEffectComp sender);

        #endregion

        public List<StatusEffect> Effects = new List<StatusEffect>();

        public StatusEffectComp()
        {
            Family = ComponentFamily.StatusEffects;
        }

        public override Type StateType
        {
            get { return typeof (StatusEffectComponentState); }
        }

        public event StatusEffectsChangedHandler Changed;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (StatusEffect effect in Effects.ToArray())
                effect.OnUpdate();
        }

        private void AddEffect(string typeName, uint uid, bool doesExpire, DateTime expiresAt,
                               StatusEffectFamily _family)
            //Don't manually use this clientside. The server adds and removes what is needed.
        {
            Type t = Type.GetType("CGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof (StatusEffect))) return;
            var newEffect = (StatusEffect) Activator.CreateInstance(t, new object[] {uid, Owner});
            newEffect.doesExpire = doesExpire;
            newEffect.expiresAt = expiresAt;
            newEffect.family = _family;
            Effects.Add(newEffect);
            newEffect.OnAdd();
            if (Changed != null) Changed(this);
        }

        private void RemoveEffect(uint uid)
            //Don't manually use this clientside. The server adds and removes what is needed.
        {
            StatusEffect toRemove = Effects.FirstOrDefault(x => x.uid == uid);
            if (toRemove != null)
            {
                toRemove.OnRemove();
                Effects.Remove(toRemove);
                if (Changed != null) Changed(this);
            }
        }

        public bool HasEffect(string typeName)
        {
            foreach (StatusEffect effect in Effects)
                if (effect.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        }

        private StatusEffect GetEffect(uint uid)
        {
            return Effects.FirstOrDefault(e => e.uid == uid);
        }

        public bool HasFamily(StatusEffectFamily family)
        {
            foreach (StatusEffect effect in Effects)
                if (effect.family == family)
                    return true;
            return false;
        }

        public override void HandleComponentState(dynamic state)
        {
            List<uint> existing = Effects.Select(uid => uid.uid).ToList();
            foreach (StatusEffectState effectState in state.EffectStates)
            {
                if (existing.Contains(effectState.Uid))
                {
                    // Effect exists, update it
                    StatusEffect existingEffect = GetEffect(effectState.Uid);
                    existingEffect.UpdateEffectState(effectState);
                }
                else
                {
                    //Effect doesn't exist, create it
                    AddEffect(effectState.TypeName, effectState.Uid, effectState.DoesExpire, effectState.ExpiresAt,
                              effectState.Family);
                }

                existing.Remove(effectState.Uid);
                // Whittle down the list so we can remove the effects that aren't contained in the state.
            }

            foreach (uint u in existing)
            {
                RemoveEffect(u);
                //The server did not send anything else, so we can remove whatever is left. No client-side only effects.
            }
        }
    }
}