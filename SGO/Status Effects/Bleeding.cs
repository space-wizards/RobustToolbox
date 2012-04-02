using System.Diagnostics;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class Bleeding : StatusEffect
    {
        private readonly Stopwatch bleedDmgTimer = new Stopwatch();
        private readonly Stopwatch bleedEntTimer = new Stopwatch();

        public Bleeding(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            : base(_uid, _affected, duration, arguments)
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
                affected.SendMessage(this, ComponentMessageType.Damage, affected, 1, DamageType.Untyped, BodyPart.Torso);
            }

            if (bleedEntTimer.ElapsedMilliseconds >= 1500)
            {
                bleedEntTimer.Restart();
                EntityManager.Singleton.SpawnEntityAt("Blood", affected.position);
            }
        }
    }
}