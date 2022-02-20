using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public sealed class MetaDataSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, ComponentHandleState>(OnMetaDataHandle);
        SubscribeLocalEvent<MetaDataComponent, ComponentGetState>(OnMetaDataGetState);
    }

    private void OnMetaDataGetState(EntityUid uid, MetaDataComponent component, ref ComponentGetState args)
    {
        args.State = new MetaDataComponentState(component._entityName, component._entityDescription, component._entityPrototype?.ID);
    }

    private void OnMetaDataHandle(EntityUid uid, MetaDataComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MetaDataComponentState state)
            return;

        component._entityName = state.Name;
        component._entityDescription = state.Description;

        if(state.PrototypeId != null)
            component._entityPrototype = _proto.Index<EntityPrototype>(state.PrototypeId);
    }

    public void AddFlag(MetaDataComponent component, MetaDataFlags flags)
    {
        component.Flags |= flags;
    }

    /// <summary>
    /// Attempts to remove the specific flag from metadata.
    /// Other systems can choose not to allow the removal if it's still relevant.
    /// </summary>
    public void RemoveFlag(MetaDataComponent component, MetaDataFlags flags)
    {
        if ((component.Flags & flags) == 0x0) return;

        var ev = new MetaFlagRemoveAttemptEvent();
        EntityManager.EventBus.RaiseLocalEvent(component.Owner, ref ev);

        if (ev.Cancelled) return;

        component.Flags &= ~flags;
    }
}

[ByRefEvent]
public struct MetaFlagRemoveAttemptEvent
{
    public bool Cancelled = false;
}
