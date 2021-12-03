using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public class IgnorePauseComponent : Component
    {
        public override string Name => "IgnorePause";

        protected override void OnAdd()
        {
            base.OnAdd();
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityPaused = false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (IoCManager.Resolve<IPauseManager>().IsMapPaused(Owner.Transform.MapID))
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityPaused = true;
            }
        }
    }
}
