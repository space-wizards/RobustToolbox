using SS14.Shared.GameObjects;
using System;

namespace SS14.Client.GameObjects
{
    public class ToxinInhalation : StatusEffect
    {
        public ToxinInhalation(uint _uid, Entity _affected)
            : base(_uid, _affected)
        {
            name = "Toxin Inhalation";
            description = "You have inhaled toxins." + Environment.NewLine + "Causes additional damage over time.";
            icon = "status_toxinh";
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