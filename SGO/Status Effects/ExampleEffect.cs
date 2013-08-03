using GameObject;
using SS13_Shared.GO;

namespace SGO
{
    public class ExampleEffect : StatusEffect
    {
        public ExampleEffect(uint _uid, Entity _affected, uint duration = 0, params object[] arguments)
            //Do not add more parameters to the constructors or bad things happen.
            : base(_uid, _affected, duration, arguments)
        {
            isDebuff = false;
            isUnique = false;
            family = StatusEffectFamily.None;
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