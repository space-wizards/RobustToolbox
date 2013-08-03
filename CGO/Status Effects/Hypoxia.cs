using System;
using GameObject;

namespace CGO
{
    public class Hypoxia : StatusEffect
    {
        public Hypoxia(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Hypoxia";
            description = "You feel your chest aching." + Environment.NewLine + "Find some air, quick!";
            icon = "status_hypoxia";
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
