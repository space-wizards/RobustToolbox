using SS13_Shared.GO;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class ExampleAction : PlayerAction
    {
        public ExampleAction(uint _uid, PlayerActionComp _parent)
            : base(_uid, _parent)
        {
        }

        public override void OnUse(IEntity targetEnt)
        {
            if (targetEnt.HasComponent(ComponentFamily.StatusEffects)) //Use component messages instead.
            {
                var statComp = (StatusEffectComp) targetEnt.GetComponent(ComponentFamily.StatusEffects);
                statComp.AddEffect("Bleeding", 10);
                parent.StartCooldown(this);
            }
        }
    }
}