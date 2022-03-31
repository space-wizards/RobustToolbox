using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects;

public abstract class PauseSystem : EntitySystem
{
    [Dependency] private readonly IMapManagerInternal _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgnorePauseComponent, ComponentAdd>(OnAdd);
        SubscribeLocalEvent<IgnorePauseComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, IgnorePauseComponent component, ComponentRemove args)
    {
        MetaData(uid).EntityPaused = _mapManager.IsMapPaused(Transform(uid).MapID);
    }

    private void OnAdd(EntityUid uid, IgnorePauseComponent component, ComponentAdd args)
    {
        MetaData(uid).EntityPaused = false;
    }
}
