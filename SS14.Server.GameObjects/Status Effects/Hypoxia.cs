using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Diagnostics;

namespace SS14.Server.GameObjects
{
    public class Hypoxia : StatusEffect
    {
        private readonly Stopwatch hypoxiaDmgTimer = new Stopwatch();
        private readonly Stopwatch hypoxiaEntTimer = new Stopwatch();

        public Hypoxia(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            : base(_uid, _affected, duration, arguments)
        {
            isDebuff = true;
            isUnique = true;
            family = StatusEffectFamily.Damage;
        }

        public override void OnAdd()
        {
            hypoxiaDmgTimer.Restart();
            hypoxiaEntTimer.Restart();
        }

        public override void OnRemove()
        {
            hypoxiaDmgTimer.Stop();
            hypoxiaEntTimer.Stop();
        }

        public override void OnUpdate()
        {
            if (hypoxiaDmgTimer.ElapsedMilliseconds >= 300)
            {
                hypoxiaDmgTimer.Restart();
                affected.SendMessage(this, ComponentMessageType.Damage, affected, 1, DamageType.Untyped, BodyPart.Torso);
            }

            if (hypoxiaEntTimer.ElapsedMilliseconds >= 1500)
            {
                hypoxiaEntTimer.Restart();
            }
        }
    }
}