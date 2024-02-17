using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

public sealed class PvsOverrideSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly HashSet<EntityUid> _hasOverride = new();

    internal HashSet<EntityUid> GlobalOverride = new();
    internal HashSet<EntityUid> ForceSend = new();
    internal Dictionary<ICommonSession, HashSet<EntityUid>> SessionOverrides = new();
    internal Dictionary<ICommonSession, HashSet<EntityUid>> SessionForceSend = new();

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EntityDeleted += OnDeleted;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<MapChangedEvent>(OnMapChanged);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridCreated);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs ev)
    {
        if (ev.NewStatus != SessionStatus.Disconnected)
            return;

        SessionOverrides.Remove(ev.Session);
        SessionForceSend.Remove(ev.Session);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityDeleted -= OnDeleted;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnDeleted(Entity<MetaDataComponent> entity)
    {
        Clear(entity);
    }

    private void Clear(EntityUid uid)
    {
        if (!_hasOverride.Remove(uid))
            return;

        ForceSend.Remove(uid);
        GlobalOverride.Remove(uid);
        foreach (var (session, set) in SessionOverrides)
        {
            if (set.Remove(uid) && set.Count == 0)
                SessionOverrides.Remove(session);
        }

        foreach (var (session, set) in SessionForceSend)
        {
            if (set.Remove(uid) && set.Count == 0)
                SessionForceSend.Remove(session);
        }
    }

    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations,
    /// causing them to always be sent to all clients.
    /// </summary>
    public void AddGlobalOverride(EntityUid uid)
    {
        if (GlobalOverride.Add(uid))
            _hasOverride.Add(uid);
    }

    /// <summary>
    /// Removes an entity from the global overrides.
    /// </summary>
    public void RemoveGlobalOverride(EntityUid uid)
    {
        GlobalOverride.Remove(uid);
        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }

    /// <summary>
    /// This causes an entity and all of its parents to always be sent to all players.
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="AddGlobalOverride"/> as it does not send children, and will ignore a players usual
    /// PVS budget. You generally shouldn't use this unless an entity absolutely always needs to be sent to all clients.
    /// </remarks>
    public void AddForceSend(EntityUid uid)
    {
        if (ForceSend.Add(uid))
            _hasOverride.Add(uid);
    }

    public void RemoveForceSend(EntityUid uid)
    {
        ForceSend.Remove(uid);
        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }

    /// <summary>
    /// This causes an entity and all of its parents to always be sent to a player..
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="AddSessionOverride"/> as it does not send children, and will ignore a players usual
    /// PVS budget. You generally shouldn't use this unless an entity absolutely always needs to be sent to a client.
    /// </remarks>
    public void AddForceSend(EntityUid uid, ICommonSession session)
    {
        if (SessionForceSend.GetOrNew(session).Add(uid))
            _hasOverride.Add(uid);
    }

    /// <summary>
    /// Removes an entity from a session's force send set.
    /// </summary>
    public void RemoveForceSend(EntityUid uid, ICommonSession session)
    {
        if (!SessionForceSend.TryGetValue(session, out var overrides))
            return;

        if (overrides.Remove(uid) && overrides.Count == 0)
            SessionForceSend.Remove(session);

        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }


    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations for a
    /// specific session.
    /// </summary>
    public void AddSessionOverride(EntityUid uid, ICommonSession session)
    {
        if (SessionOverrides.GetOrNew(session).Add(uid))
            _hasOverride.Add(uid);
    }

    /// <summary>
    /// Removes an entity from a session's overrides.
    /// </summary>
    public void RemoveSessionOverride(EntityUid uid, ICommonSession session)
    {
        if (!SessionOverrides.TryGetValue(session, out var overrides))
            return;

        if (overrides.Remove(uid) && overrides.Count == 0)
            SessionOverrides.Remove(session);

        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }

    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations,
    /// causing them to always be sent to all clients.
    /// </summary>
    public void AddSessionOverrides(EntityUid uid, Filter filter)
    {
        foreach (var session in filter.Recipients)
        {
            AddSessionOverride(uid, session);
        }
    }

    [Obsolete("Use variant that takes in an EntityUid")]
    public void AddGlobalOverride(NetEntity entity, bool removeExistingOverride = true, bool recursive = false)
    {
        if (TryGetEntity(entity, out var uid))
            AddGlobalOverride(uid.Value);
    }

    [Obsolete("Use variant that takes in an EntityUid")]
    public void AddSessionOverride(NetEntity entity, ICommonSession session, bool removeExistingOverride = true)
    {
        if (TryGetEntity(entity, out var uid))
            AddSessionOverride(uid.Value, session);
    }

    [Obsolete("Use variant that takes in an EntityUid")]
    public void AddSessionOverrides(NetEntity entity, Filter filter, bool removeExistingOverride = true)
    {
        if (TryGetEntity(entity, out var uid))
            AddSessionOverrides(uid.Value, filter);
    }

    [Obsolete("Don't use this, clear specific overrides")]
    public void ClearOverride(NetEntity entity)
    {
        if (TryGetEntity(entity, out var uid))
            Clear(uid.Value);
    }

    #region Map/Grid Events

    private void OnMapChanged(MapChangedEvent ev)
    {
        if (ev.Created)
            OnMapCreated(ev);
        else
            OnMapDestroyed(ev);
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        RemoveForceSend(ev.EntityUid);
    }

    private void OnGridCreated(GridInitializeEvent ev)
    {
        // TODO PVS remove this requirement.
        // I think this just required refactoring client game state logic so it doesn't send grids to nullspace?
        AddForceSend(ev.EntityUid);
    }

    private void OnMapDestroyed(MapChangedEvent ev)
    {
        RemoveForceSend(ev.Uid);
    }

    private void OnMapCreated(MapChangedEvent ev)
    {
        // TODO PVS remove this requirement.
        // I think this just required refactoring client game state logic so it doesn't sending maps/grids to nullspace.
        AddForceSend(ev.Uid);
    }

    #endregion
}
