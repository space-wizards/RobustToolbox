using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;
using System;
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;

namespace CGO
{
    public class StatusEffectComp : GameObjectComponent
    {
        public override ComponentFamily Family { get { return ComponentFamily.StatusEffects; } }

        public delegate void StatusEffectsChangedHandler(StatusEffectComp sender);
        public event StatusEffectsChangedHandler Changed;

        public List<StatusEffect> Effects = new List<StatusEffect>();

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.AddStatusEffect):
                    var typeName = (string)message.MessageParameters[1];
                    var uid = (uint)message.MessageParameters[2];
                    var doesExpire = (bool)message.MessageParameters[3];
                    var expireSeconds = (double)message.MessageParameters[4];
                    var family = (int)message.MessageParameters[5];
                    AddEffect(typeName, uid, doesExpire, DateTime.Now.AddSeconds(expireSeconds), (StatusEffectFamily)family);
                    break;

                case (ComponentMessageType.RemoveStatusEffect):
                    var uid2 = (uint)message.MessageParameters[1];
                    RemoveEffect(uid2);
                    break;

                default:
                    base.HandleNetworkMessage(message);
                    break;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (StatusEffect effect in Effects.ToArray())
                effect.OnUpdate();
        }

        private void AddEffect(string typeName, uint uid, bool doesExpire, DateTime expiresAt, StatusEffectFamily _family) //Don't manually use this clientside. The server adds and removes what is needed.
        {
            Type t = Type.GetType("CGO." + typeName);
            if (t == null || !t.IsSubclassOf(typeof(StatusEffect))) return;
            StatusEffect newEffect = (StatusEffect)Activator.CreateInstance(t, new object[] { uid, this.Owner });
            newEffect.doesExpire = doesExpire;
            newEffect.expiresAt = expiresAt;
            newEffect.family = _family;
            Effects.Add(newEffect);
            newEffect.OnAdd();
            if (Changed != null) Changed(this);
        }

        private void RemoveEffect(uint uid) //Don't manually use this clientside. The server adds and removes what is needed.
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

        public bool HasFamily(StatusEffectFamily family)
        {
            foreach (StatusEffect effect in Effects)
                if (effect.family == family)
                    return true;
            return false;
        }
    }
}
