using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

public sealed class PvsOverrideSystem : SharedPvsOverrideSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

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
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);
        SubscribeLocalEvent<MapCreatedEvent>(OnMapCreated);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridCreated);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

        // TODO console commands for adding/removing overrides?
        _console.RegisterCommand(
            "pvs_override_info",
            Loc.GetString("cmd-pvs-override-info-desc"),
            "pvs_override_info",
            GetPvsInfo,
            GetCompletion);
    }

    #region Console Commands

    /// <summary>
    /// Debug command for displaying PVS override information.
    /// </summary>
    private void GetPvsInfo(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var nuid) || !TryGetEntity(nuid, out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid"));
            return;
        }

        if (!_hasOverride.Contains(uid.Value))
        {
            shell.WriteLine(Loc.GetString("cmd-pvs-override-info-empty", ("nuid", args[0])));
            return;
        }

        if (GlobalOverride.Contains(uid.Value) || ForceSend.Contains(uid.Value))
            shell.WriteLine(Loc.GetString("cmd-pvs-override-info-global", ("nuid", args[0])));

        HashSet<ICommonSession> sessions = new();
        sessions.UnionWith(SessionOverrides.Where(x => x.Value.Contains(uid.Value)).Select(x => x.Key));
        sessions.UnionWith(SessionForceSend.Where(x => x.Value.Contains(uid.Value)).Select(x => x.Key));
        if (sessions.Count == 0)
            return;

        var clients = string.Join(", ", sessions.Select(x => x.ToString()));
        shell.WriteLine(Loc.GetString("cmd-pvs-override-info-clients", ("nuid", args[0]), ("clients", clients)));
    }

    private CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.NetEntities(args[0], EntityManager), "NetEntity");
    }

    #endregion

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
    /// causing them to be sent to all clients. This will still respect visibility masks, it only overrides the range.
    /// </summary>
    public override void AddGlobalOverride(EntityUid uid)
    {
        base.AddGlobalOverride(uid);

        if (GlobalOverride.Add(uid))
            _hasOverride.Add(uid);
    }

    /// <summary>
    /// Removes an entity from the global overrides.
    /// </summary>
    public override void RemoveGlobalOverride(EntityUid uid)
    {
        base.RemoveGlobalOverride(uid);

        GlobalOverride.Remove(uid);
        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }

    /// <summary>
    /// This causes an entity and all of its parents to always be sent to all players.
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="AddGlobalOverride"/> as it does not send children, will ignore a players usual
    /// PVS budget, and ignores visibility masks. You generally shouldn't use this unless an entity absolutely always
    /// needs to be sent to all clients.
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
    /// This causes an entity and all of its parents to always be sent to a player.
    /// </summary>
    /// <remarks>
    /// This differs from <see cref="AddSessionOverride"/> as it does not send children, will ignore a players usual
    /// PVS budget, and ignores visibility masks. You generally shouldn't use this unless an entity absolutely always
    /// needs to be sent to a client.
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
    /// specific session. This will still respect visibility masks, it only overrides the range.
    /// </summary>
    public override void AddSessionOverride(EntityUid uid, ICommonSession session)
    {
        base.AddSessionOverride(uid, session);

        if (SessionOverrides.GetOrNew(session).Add(uid))
            _hasOverride.Add(uid);
    }

    /// <summary>
    /// Removes an entity from a session's overrides.
    /// </summary>
    public override void RemoveSessionOverride(EntityUid uid, ICommonSession session)
    {
        base.RemoveSessionOverride(uid, session);

        if (!SessionOverrides.TryGetValue(session, out var overrides))
            return;

        if (overrides.Remove(uid) && overrides.Count == 0)
            SessionOverrides.Remove(session);

        // Not bothering to clear _hasOverride, as we'd have to check all the other collections, and at that point we
        // might as well just do that when the entity gets deleted anyways.
    }

    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations,
    /// causing them to always be sent to the specified clients. This will still respect visibility masks, it only
    /// overrides the range.
    /// </summary>
    public override void AddSessionOverrides(EntityUid uid, Filter filter)
    {
        _hasOverride.Add(uid);
        base.AddSessionOverrides(uid, filter);

        foreach (var session in filter.Recipients)
        {
            SessionOverrides.GetOrNew(session).Add(uid);
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

    [Obsolete("Don't use this, clear specific overrides")]
    public void ClearOverride(NetEntity entity)
    {
        if (TryGetEntity(entity, out var uid))
            Clear(uid.Value);
    }

    #region Map/Grid Events

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

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        RemoveForceSend(ev.Uid);
    }

    private void OnMapCreated(MapCreatedEvent ev)
    {
        // TODO PVS remove this requirement.
        // I think this just required refactoring client game state logic so it doesn't sending maps/grids to nullspace.
        AddForceSend(ev.Uid);
    }

    #endregion
}
