using Robust.Server.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIgnorePauseComponent))]
    public sealed class IgnorePauseComponent : SharedIgnorePauseComponent
    {
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
