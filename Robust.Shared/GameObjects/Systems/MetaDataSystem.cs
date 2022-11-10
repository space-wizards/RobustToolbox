using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class MetaDataSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, ComponentHandleState>(OnMetaDataHandle);
        SubscribeLocalEvent<MetaDataComponent, ComponentGetState>(OnMetaDataGetState);
    }

    private void OnMetaDataGetState(EntityUid uid, MetaDataComponent component, ref ComponentGetState args)
    {
        args.State = new MetaDataComponentState(component._entityName, component._entityDescription, component._entityPrototype?.ID, component.PauseTime);
    }

    private void OnMetaDataHandle(EntityUid uid, MetaDataComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MetaDataComponentState state)
            return;

        component._entityName = state.Name;
        component._entityDescription = state.Description;

        if(state.PrototypeId != null && state.PrototypeId != component._entityPrototype?.ID)
            component._entityPrototype = _proto.Index<EntityPrototype>(state.PrototypeId);

        component.PauseTime = state.PauseTime;
    }

    public bool EntityPaused(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return true;

        return metadata.EntityPaused;
    }

    public void SetEntityPaused(EntityUid uid, bool value, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata)) return;

        if (metadata.EntityPaused == value) return;

        if (value)
        {
            DebugTools.Assert(metadata.PauseTime == null);
            metadata.PauseTime = _timing.CurTime;
        }
        else
        {
            DebugTools.Assert(metadata.PauseTime != null);
            metadata.PauseTime = null;
        }

        RaiseLocalEvent(uid, new EntityPausedEvent(uid, value));
        Dirty(metadata);
    }

    /// <summary>
    /// Gets how long this entity has been paused.
    /// </summary>
    public TimeSpan GetPauseTime(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return TimeSpan.Zero;

        return (_timing.CurTime - metadata.PauseTime) ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Offsets the specified time by how long the entity has been paused.
    /// </summary>
    public void PauseOffset(EntityUid uid, ref TimeSpan time, MetaDataComponent? metadata = null)
    {
        var paused = GetPauseTime(uid, metadata);
        time += paused;
    }

    public void AddFlag(EntityUid uid, MetaDataFlags flags, MetaDataComponent? component = null)
    {
        if (!Resolve(uid, ref component)) return;

        component.Flags |= flags;
    }

    /// <summary>
    /// Attempts to remove the specific flag from metadata.
    /// Other systems can choose not to allow the removal if it's still relevant.
    /// </summary>
    public void RemoveFlag(EntityUid uid, MetaDataFlags flags, MetaDataComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var toRemove = component.Flags & flags;
        if (toRemove == 0x0)
            return;

        var ev = new MetaFlagRemoveAttemptEvent(toRemove);
        EntityManager.EventBus.RaiseLocalEvent(component.Owner, ref ev, true);

        component.Flags &= ~ev.ToRemove;
    }

    public virtual void SetVisibilityMask(EntityUid uid, int value, MetaDataComponent? meta = null)
    {
        if (Resolve(uid, ref meta))
            meta.VisibilityMask = value;
    }
}

/// <summary>
/// Raised if <see cref="MetaDataSystem"/> is trying to remove a particular flag.
/// </summary>
[ByRefEvent]
public struct MetaFlagRemoveAttemptEvent
{
    public MetaDataFlags ToRemove;

    public MetaFlagRemoveAttemptEvent(MetaDataFlags toRemove)
    {
        ToRemove = toRemove;
    }
}
