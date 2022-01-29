using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed class IgnorePauseComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        protected override void OnAdd()
        {
            base.OnAdd();
            _entMan.GetComponent<MetaDataComponent>(Owner).EntityPaused = false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (IoCManager.Resolve<IPauseManager>().IsMapPaused(_entMan.GetComponent<TransformComponent>(Owner).MapID))
            {
                _entMan.GetComponent<MetaDataComponent>(Owner).EntityPaused = true;
            }
        }
    }
}
