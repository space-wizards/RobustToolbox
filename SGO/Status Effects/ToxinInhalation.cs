using System.Diagnostics;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class ToxinInhalation : StatusEffect
    {
        private readonly Stopwatch toxDmgTimer = new Stopwatch();
        private readonly Stopwatch toxEntTimer = new Stopwatch();

        public ToxinInhalation(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            : base(_uid, _affected, duration, arguments)
        {
            isDebuff = true;
            isUnique = true;
            family = StatusEffectFamily.Damage;
        }

        public override void OnAdd()
        {
            toxDmgTimer.Restart();
            toxEntTimer.Restart();
        }

        public override void OnRemove()
        {
            toxDmgTimer.Stop();
            toxEntTimer.Stop();
        }

        public override void OnUpdate()
        {
            if (toxDmgTimer.ElapsedMilliseconds >= 300)
            {
                toxDmgTimer.Restart();
                affected.SendMessage(this, ComponentMessageType.Damage, affected, 1, DamageType.Untyped, BodyPart.Torso);
            }

            if (toxEntTimer.ElapsedMilliseconds >= 1500)
            {
                toxEntTimer.Restart();
            }
        }
    }
}