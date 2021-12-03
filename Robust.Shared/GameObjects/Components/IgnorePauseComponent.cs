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
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner).EntityPaused = false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (IoCManager.Resolve<IPauseManager>().IsMapPaused(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).MapID))
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner).EntityPaused = true;
            }
        }
    }
}
