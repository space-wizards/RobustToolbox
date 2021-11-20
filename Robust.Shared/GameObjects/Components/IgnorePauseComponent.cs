using System;

namespace Robust.Shared.GameObjects
{
    [Obsolete("Set Entity.MetaData.IgnorePaused")]
    [RegisterComponent]
    public class IgnorePauseComponent : Component
    {
        public override string Name => "IgnorePause";

        protected override void OnAdd()
        {
            base.OnAdd();

            Owner.MetaData.IgnorePaused = true;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            Owner.MetaData.IgnorePaused = false;
        }
    }
}
