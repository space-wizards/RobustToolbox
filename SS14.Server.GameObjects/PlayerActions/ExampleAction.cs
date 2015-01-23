using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Server.GameObjects
{
    public class ExampleAction : PlayerAction
    {
        public ExampleAction(uint _uid, PlayerActionComp _parent)
            : base(_uid, _parent)
        {
        }

        public override void OnUse(Entity targetEnt)
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