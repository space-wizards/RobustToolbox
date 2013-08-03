using GameObject;

namespace CGO
{
    public class Rooted : StatusEffect
    {
        public Rooted(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Rooted";
            description = "You can not move.";
            icon = "status_stun";
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
