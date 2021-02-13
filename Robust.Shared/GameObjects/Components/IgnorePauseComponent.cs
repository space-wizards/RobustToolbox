using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public class IgnorePauseComponent : Component
    {
        public override string Name => "IgnorePause";

        public override void OnAdd()
        {
            base.OnAdd();
            Owner.Paused = false;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (IoCManager.Resolve<IPauseManager>().IsMapPaused(Owner.Transform.MapID))
            {
                Owner.Paused = true;
            }
        }
    }
}
