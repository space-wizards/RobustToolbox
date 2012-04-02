using SS13_Shared.GO;

namespace SGO
{
    public class Rooted : StatusEffect
    {
        public Rooted(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            : base(_uid, _affected, duration, arguments)
        {
            isDebuff = true;
            isUnique = true;
            family = StatusEffectFamily.Root;
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