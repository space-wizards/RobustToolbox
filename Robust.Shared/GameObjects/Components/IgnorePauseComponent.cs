using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed class IgnorePauseComponent : Component
    {
        protected override void OnAdd()
        {
            base.OnAdd();
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner).EntityPaused = false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (IoCManager.Resolve<IMapManager>().IsMapPaused(entMan.GetComponent<TransformComponent>(Owner).MapID))
            {
                entMan.GetComponent<MetaDataComponent>(Owner).EntityPaused = true;
            }
        }
    }
}
