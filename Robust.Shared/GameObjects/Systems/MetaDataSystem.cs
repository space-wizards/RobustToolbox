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

    private EntityPausedEvent _pausedEvent;

    private EntityQuery<MetaDataComponent> _metaQuery;

    public override void Initialize()
    {
        _metaQuery = GetEntityQuery<MetaDataComponent>();
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

    public void SetEntityName(EntityUid uid, string value, MetaDataComponent? metadata = null, bool raiseEvents = true)
    {
        if (!_metaQuery.Resolve(uid, ref metadata) || value.Equals(metadata.EntityName))
            return;

        var oldName = metadata.EntityName;

        metadata._entityName = value;

        if (raiseEvents)
        {
            var ev = new EntityRenamedEvent(uid, oldName, value);
            RaiseLocalEvent(uid, ref ev, true);
        }

        Dirty(uid, metadata, metadata);
    }

    public void SetEntityDescription(EntityUid uid, string value, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata) || value.Equals(metadata.EntityDescription))
            return;

        metadata._entityDescription = value;
        Dirty(uid, metadata, metadata);
    }

    internal void SetEntityPrototype(EntityUid uid, EntityPrototype? value, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata) || value?.Equals(metadata._entityPrototype) == true)
            return;

        // The ID string should never change after an entity has been created.
        // Otherwise this breaks networking in multiplayer games.
        DebugTools.Assert(value?.ID == metadata._entityPrototype?.ID);

        metadata._entityPrototype = value;
    }

    public bool EntityPaused(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata))
            return true;

        return metadata.EntityPaused;
    }

    public void SetEntityPaused(EntityUid uid, bool value, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata)) return;

        if (metadata.EntityPaused == value) return;

        if (value)
        {
            DebugTools.Assert(metadata.PauseTime == null);
            metadata.PauseTime = _timing.CurTime;
            RaiseLocalEvent(uid, ref _pausedEvent);
        }
        else
        {
            DebugTools.Assert(metadata.PauseTime != null);
            var ev = new EntityUnpausedEvent(_timing.CurTime - metadata.PauseTime!.Value);
            metadata.PauseTime = null;
            RaiseLocalEvent(uid, ref ev);
        }

        Dirty(uid, metadata, metadata);
    }

    /// <summary>
    /// Gets how long this entity has been paused.
    /// </summary>
    public TimeSpan GetPauseTime(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata))
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

    public void SetFlag(Entity<MetaDataComponent?> entity, MetaDataFlags flags, bool enabled)
    {
        if (!_metaQuery.Resolve(entity, ref entity.Comp))
            return;

        if (enabled)
            entity.Comp.Flags |= flags;
        else
            RemoveFlag(entity, flags, entity.Comp);
    }

    public void AddFlag(EntityUid uid, MetaDataFlags flags, MetaDataComponent? comp = null)
        => SetFlag((uid, comp), flags, true);

    /// <summary>
    /// Attempts to remove the specific flag from metadata.
    /// Other systems can choose not to allow the removal if it's still relevant.
    /// </summary>
    public void RemoveFlag(EntityUid uid, MetaDataFlags flags, MetaDataComponent? component = null)
    {
        if (!_metaQuery.Resolve(uid, ref component))
            return;

        var toRemove = component.Flags & flags;
        if (toRemove == 0x0)
            return;

        // TODO PERF
        // does this need to be a broadcast event?
        var ev = new MetaFlagRemoveAttemptEvent(toRemove);
        RaiseLocalEvent(uid, ref ev, true);

        component.Flags &= ~ev.ToRemove;
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
