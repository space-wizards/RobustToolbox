using SS14.Shared.GameObjects;
using System;

namespace SS14.Client.GameObjects
{
    public class Bleeding : StatusEffect
    {
        public Bleeding(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Bleeding";
            description = "You are bleeding." + Environment.NewLine + "Causes additional damage over time.";
            icon = "status_bleeding";
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