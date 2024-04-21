using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transforms = default!;

    /*
     * TODO:
     * Need the external call methods that raise the event as a predicted event(?)
     * Need to be able to call open in a shared context.
     * When changing mob need to close old UIs and open new ones (internally, don't call the event?)
     * All events get raised shared maybe? Like uhh open UI or close UI or interact with it
     * Server messages only get sent to relevant client.
     */

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<IgnoreUIRangeComponent> _ignoreUIRangeQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<UserInterfaceComponent> _uiQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();
        _ignoreUIRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _uiQuery = GetEntityQuery<UserInterfaceComponent>();

        SubscribeAllEvent<BoundUIWrapMessage>(OnMessageReceived);

        SubscribeLocalEvent<UserInterfaceComponent, OpenBoundInterfaceMessage>(OnUserInterfaceOpen);
        SubscribeLocalEvent<UserInterfaceComponent, CloseBoundInterfaceMessage>(OnUserInterfaceClosed);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentGetState>(OnUserInterfaceGetState);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentHandleState>(OnUserInterfaceHandleState);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(ref PlayerAttachedEvent ev)
    {
        if (!_actorQuery.TryGetComponent(ev.Entity, out var actor))
            return;

        // Open BUIs upon attachment
        foreach (var (uid, keys) in actor.OpenInterfaces)
        {
            if (!_uiQuery.TryGetComponent(uid, out var uiComp))
                continue;

            foreach (var key in keys)
            {
                if (!uiComp.Interfaces.TryGetValue(key, out var data))
                    continue;

                EnsureClientBui((uid, uiComp), key, data);
            }
        }
    }

    private void OnPlayerDetached(ref PlayerDetachedEvent ev)
    {
        if (!_actorQuery.TryGetComponent(ev.Entity, out var actor))
            return;

        // Close BUIs open detachment.
        foreach (var (uid, keys) in actor.OpenInterfaces)
        {
            if (!_uiQuery.TryGetComponent(uid, out var uiComp))
                continue;

            foreach (var key in keys)
            {
                if (!uiComp.ClientOpenInterfaces.TryGetValue(key, out var cBui))
                    continue;

                cBui.Close();
                uiComp.ClientOpenInterfaces.Remove(key);
            }
        }
    }

    private void OnUserInterfaceClosed(Entity<UserInterfaceComponent> ent, ref CloseBoundInterfaceMessage args)
    {
        ent.Comp.Actors[args.UiKey].Remove(GetEntity(args.Entity));
        Dirty(ent);

        var actor = GetEntity(args.Entity);

        if (_actorQuery.TryGetComponent(actor, out var actorComp) &&
            actorComp.OpenInterfaces.TryGetValue(ent.Owner, out var keys))
        {
            keys.Remove(args.UiKey);

            if (keys.Count == 0)
                actorComp.OpenInterfaces.Remove(ent.Owner);
        }

        // If we're client we want this handled immediately.
        if (ent.Comp.ClientOpenInterfaces.Remove(args.UiKey, out var cBui))
        {
            cBui.Close();
        }
    }

    private void OnUserInterfaceOpen(Entity<UserInterfaceComponent> ent, ref OpenBoundInterfaceMessage args)
    {
        var actor = GetEntity(args.Entity);

        if (!_actorQuery.TryGetComponent(actor, out var actorComp))
        {
            return;
        }

        // Let state handling open the UI clientside.
        actorComp.OpenInterfaces.GetOrNew(ent.Owner).Add(args.UiKey);
        ent.Comp.Actors.GetOrNew(args.UiKey).Add(actor);
        Dirty(ent);

        // If we're client we want this handled immediately.
        EnsureClientBui(ent, args.UiKey, ent.Comp.Interfaces[args.UiKey]);
    }

    private void OnUserInterfaceShutdown(EntityUid uid, UserInterfaceComponent component, ComponentShutdown args)
    {
        foreach (var bui in component.ClientOpenInterfaces.Values)
        {
            bui.Close();
        }

        component.ClientOpenInterfaces.Clear();
    }

    private void OnUserInterfaceGetState(Entity<UserInterfaceComponent> ent, ref ComponentGetState args)
    {
        var actors = new Dictionary<Enum, List<NetEntity>>();
        var states = new Dictionary<Enum, BoundUserInterfaceState>();

        foreach (var (key, acts) in ent.Comp.Actors)
        {
            actors[key] = GetNetEntityList(acts);
        }

        foreach (var (key, state) in ent.Comp.States)
        {
            states[key] = state;
        }

        args.State = new UserInterfaceComponent.UserInterfaceComponentState(actors, states);
    }

    private void OnUserInterfaceHandleState(Entity<UserInterfaceComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not UserInterfaceComponent.UserInterfaceComponentState state)
            return;

        var toRemove = new ValueList<Enum>();

        foreach (var (key, actors) in state.Actors)
        {
            var existing = CollectionsMarshal.GetValueRefOrAddDefault(ent.Comp.Actors, key, out _);
            existing!.Clear();
            existing.AddRange(EnsureEntityList<UserInterfaceComponent>(actors, ent.Owner));
        }

        // State handling
        foreach (var key in ent.Comp.States.Keys)
        {
            if (state.States.ContainsKey(key))
                continue;

            toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            ent.Comp.States.Remove(key);
        }

        toRemove.Clear();

        // Add new states / update existing ones.
        foreach (var (key, buiState) in state.States)
        {
            if (!state.States.TryGetValue(key, out var existing) ||
                existing.Equals(buiState))
            {
                continue;
            }

            state.States[key] = buiState;

            if (!ent.Comp.ClientOpenInterfaces.TryGetValue(key, out var cBui))
                continue;

            cBui.UpdateState(buiState);
        }

        // Check if the UI is still open, otherwise call close.
        foreach (var (key, bui) in ent.Comp.ClientOpenInterfaces)
        {
            if (ent.Comp.Actors.ContainsKey(key))
                continue;

            bui.Close();
            toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            ent.Comp.ClientOpenInterfaces.Remove(key);
        }

        // If UI not open then open it
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != null)
        {
            foreach (var (key, value) in ent.Comp.Interfaces)
            {
                EnsureClientBui(ent, key, value);
            }
        }
    }

    private void EnsureClientBui(Entity<UserInterfaceComponent> entity, Enum key, InterfaceData data)
    {
        // If it's out BUI open it up and apply the state, otherwise do nothing.
        var player = _player.LocalEntity;

        if (player == null ||
            !entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(player.Value))
        {
            return;
        }

        DebugTools.Assert(_netManager.IsClient);

        if (entity.Comp.ClientOpenInterfaces.ContainsKey(key))
        {
            return;
        }

        var type = _reflection.LooseGetType(data.ClientType);
        var boundUserInterface = (BoundUserInterface) _factory.CreateInstance(type, [entity.Owner, key]);

        entity.Comp.ClientOpenInterfaces[key] = boundUserInterface;
        boundUserInterface.Open();

        if (entity.Comp.States.TryGetValue(key, out var buiState))
        {
            boundUserInterface.UpdateState(buiState);
        }
    }

    public void CloseActorUis(Entity<ActorComponent?> entity)
    {
        // TODO:
        throw new NotImplementedException();
    }

    public IEnumerable<(EntityUid Entity, Enum Key)> GetActorUis(Entity<ActorComponent?> entity)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Closes the attached UI for all entities.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.Remove(key))
            return;

        Dirty(entity);
    }

    /// <summary>
    /// Closes the attached UI only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession? actor, bool predicted = true)
    {
        var actorEnt = actor?.AttachedEntity;

        if (actorEnt == null)
            return;

        CloseUi(entity, key, actorEnt.Value, predicted);
    }

    /// <summary>
    /// Closes the attached UI only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid? actor, bool predicted = true)
    {
        if (actor == null)
            return;

        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        if (!actors.Contains(actor.Value))
            return;

        if (predicted)
        {
            // Not guaranteed to open so rely upon the event handling it.
            // Also lets client request it to be opened remotely too.
            EntityManager.RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new CloseBoundInterfaceMessage(), key));
        }
        else if (!_netManager.IsClient)
        {
            RaiseLocalEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new CloseBoundInterfaceMessage(), key));
        }
    }

    public bool TryOpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor, bool predicted = true)
    {
        OpenUi(entity, key, actor, predicted);

        // Due to the event actually handling the UI open / closed we can't
        if (entity.Comp == null ||
            !entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(actor))
        {
            return false;
        }

        return true;
    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor, bool predicted = true)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || actors.Contains(actor))
            return;

        if (predicted)
        {
            // Not guaranteed to open so rely upon the event handling it.
            // Also lets client request it to be opened remotely too.
            EntityManager.RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new OpenBoundInterfaceMessage(), key));
        }
        else if (!_netManager.IsClient)
        {
            RaiseLocalEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new OpenBoundInterfaceMessage(), key));
        }
    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession actor, bool predicted = true)
    {
        var actorEnt = actor.AttachedEntity;

        if (actorEnt == null)
            return;

        OpenUi(entity, key, actorEnt.Value, predicted);
    }

    public void SetUiState(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceState? state)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        // Null state
        if (state == null)
        {
            if (!entity.Comp.States.Remove(key))
                return;

            Dirty(entity);
        }
        // Non-null state, check if it matches existing.
        else
        {
            ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(entity.Comp.States, key, out var exists);

            if (exists && stateRef?.Equals(state) == true)
                return;

            stateRef = state;
        }

        Dirty(entity);
    }

    public bool HasUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref ui))
            return false;

        return ui.Interfaces.ContainsKey(uiKey);
    }

    public bool IsUiOpen(Entity<UserInterfaceComponent?> entity, Enum uiKey)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.Actors.TryGetValue(uiKey, out var actors))
            return false;

        DebugTools.Assert(actors.Count > 0);
        return actors.Count > 0;
    }

    public bool IsUiOpen(Entity<UserInterfaceComponent?> entity, Enum uiKey, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.Actors.TryGetValue(uiKey, out var actors))
            return false;

        return actors.Contains(actor);
    }

    #region Server messages

    public void ServerSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        var filter = Filter.Entities(actors.ToArray());
        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), filter);
    }

    public void ServerSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(actor))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), actor);
    }

    public void ServerSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, ICommonSession actor)
    {
        if (!_netManager.IsClient)
            return;

        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) || actor.AttachedEntity is not { } attachedEntity)
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(attachedEntity))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), actor);
    }

    #endregion

    #region Client messages

    public void ClientSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key));
    }

    public void ClientSendPredictedMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, ICommonSession actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key));
    }

    #endregion

    /// <summary>
    /// Closes all UIs for the entity.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        entity.Comp.Actors.Clear();
        entity.Comp.States.Clear();
        Dirty(entity);
    }

    /// <summary>
    ///     Validates the received message, and then pass it onto systems/components
    /// </summary>
    internal void OnMessageReceived(BaseBoundUIWrapMessage msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Entity);

        if (!_uiQuery.TryComp(uid, out var uiComp) ||
            args.SenderSession is not { } session ||
            session.AttachedEntity is not { } attachedEntity)
        {
            return;
        }

        if (!uiComp.Interfaces.TryGetValue(msg.UiKey, out var ui))
        {
            Log.Debug($"Got BoundInterfaceMessageWrapMessage for unknown UI key: {msg.UiKey}");
            return;
        }

        // If it's not an open message check we're even a subscriber.
        if (msg.Message is not OpenBoundInterfaceMessage &&
            (!uiComp.Actors.TryGetValue(msg.UiKey, out var actors) ||
            !actors.Contains(attachedEntity)))
        {
            Log.Debug($"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {session}");
            return;
        }

        // verify that the user is allowed to press buttons on this UI:
        if (ui.RequireInputValidation)
        {
            var attempt = new BoundUserInterfaceMessageAttempt(args.SenderSession, uid, msg.UiKey);
            RaiseLocalEvent(attempt);
            if (attempt.Cancelled)
                return;
        }

        // get the wrapped message and populate it with the sender & UI key information.
        var message = msg.Message;
        message.Session = args.SenderSession;
        message.Entity = msg.Entity;
        message.UiKey = msg.UiKey;

        // Raise as object so the correct type is used.
        RaiseLocalEvent(uid, (object)message, true);
    }

    /// <summary>
    /// Tries to get the BUI if it is currently open.
    /// </summary>
    public bool TryGetOpenUi(Entity<UserInterfaceComponent?> entity, Enum uiKey, [NotNullWhen(true)] out BoundUserInterface? bui)
    {
        bui = null;

        return _uiQuery.Resolve(entity.Owner, ref entity.Comp, false) && entity.Comp.ClientOpenInterfaces.TryGetValue(uiKey, out bui);
    }

    /// <summary>
    /// Tries to get the BUI if it is currently open.
    /// </summary>
    public bool TryGetOpenUi<T>(Entity<UserInterfaceComponent?> entity, Enum uiKey, [NotNullWhen(true)] out T? bui) where T : BoundUserInterface
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) || !entity.Comp.ClientOpenInterfaces.TryGetValue(uiKey, out var cBui))
        {
            bui = null;
            return false;
        }

        bui = (T)cBui;
        return true;
    }

    public bool TryToggleUi(Entity<UserInterfaceComponent?> entity, Enum uiKey, ICommonSession actor)
    {
        if (actor.AttachedEntity is not { } attachedEntity)
            return false;

        return TryToggleUi(entity, uiKey, attachedEntity);
    }

    /// <summary>
    ///     Switches between closed and open for a specific client.
    /// </summary>
    public bool TryToggleUi(Entity<UserInterfaceComponent?> entity, Enum uiKey, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) ||
            !entity.Comp.Interfaces.ContainsKey(uiKey))
        {
            return false;
        }

        if (entity.Comp.Actors.TryGetValue(uiKey, out var actors) && actors.Contains(actor))
        {
            CloseUi(entity, uiKey, actor);
        }
        else
        {
            OpenUi(entity, uiKey, actor);
        }

        return true;
    }

    /// <summary>
    /// Raised by client-side UIs to send predicted messages to server.
    /// </summary>
    internal void SendPredictedUiMessage(BoundUserInterface bui, BoundUserInterfaceMessage msg)
    {
        RaisePredictiveEvent(new PredictedBoundUIWrapMessage(GetNetEntity(bui.Owner), msg, bui.UiKey));
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var query = AllEntityQuery<ActiveUserInterfaceComponent, UserInterfaceComponent>();

        while (query.MoveNext(out var uid, out var activeUis, out var uiComp))
        {
            foreach (var (key, actors) in uiComp.Actors)
            {
                DebugTools.Assert(actors.Count > 0);
                var data = uiComp.Interfaces[key];

                // Short-circuit
                if (data.InteractionRange <= 0f || actors.Count == 0)
                    continue;

                // Okay so somehow UISystem is high up on the server profile
                // If that's actually still a problem turn this into an IParallelRobustJob and slam all the UIs in parallel.
                var xform = _xformQuery.GetComponent(uid);
                var coordinates = xform.Coordinates;
                var mapId = xform.MapID;

                for (var i = actors.Count - 1; i >= 0; i--)
                {
                    var actor = actors[i];

                    if (CheckRange(uid, key, data, actor, coordinates, mapId))
                        continue;

                    CloseUi((uid, uiComp), key, actor);
                }
            }
        }
    }

    /// <summary>
    ///     Verify that the subscribed clients are still in range of the interface.
    /// </summary>
    private bool CheckRange(
        EntityUid uid,
        Enum key,
        InterfaceData data,
        EntityUid actor,
        EntityCoordinates uiCoordinates,
        MapId uiMap)
    {
        if (_ignoreUIRangeQuery.HasComponent(actor))
            return true;

        if (!_xformQuery.TryGetComponent(actor, out var actorXform))
            return false;

        // Handle pluggable BoundUserInterfaceCheckRangeEvent
        var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(uid, key, actor);
        RaiseLocalEvent(uid, ref checkRangeEvent, broadcast: true);

        if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Pass)
            return true;

        if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Fail)
            return false;

        DebugTools.Assert(checkRangeEvent.Result == BoundUserInterfaceRangeResult.Default);

        if (uiMap != actorXform.MapID)
            return false;

        return uiCoordinates.InRange(EntityManager, _transforms, actorXform.Coordinates, data.InteractionRange);
    }
}

/// <summary>
/// Raised by <see cref="UserInterfaceSystem"/> to check whether an interface is still accessible by its user.
/// </summary>
[ByRefEvent]
[PublicAPI]
public struct BoundUserInterfaceCheckRangeEvent
{
    /// <summary>
    /// The entity owning the UI being checked for.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    /// The UI itself.
    /// </summary>
    /// <returns></returns>
    public readonly Enum UiKey;

    /// <summary>
    /// The player for which the UI is being checked.
    /// </summary>
    public readonly EntityUid Actor;

    /// <summary>
    /// The result of the range check.
    /// </summary>
    public BoundUserInterfaceRangeResult Result;

    public BoundUserInterfaceCheckRangeEvent(
        EntityUid target,
        Enum uiKey,
        EntityUid actor)
    {
        Target = target;
        UiKey = uiKey;
        Actor = actor;
    }
}

/// <summary>
/// Possible results for a <see cref="BoundUserInterfaceCheckRangeEvent"/>.
/// </summary>
public enum BoundUserInterfaceRangeResult : byte
{
    /// <summary>
    /// Run built-in range check.
    /// </summary>
    Default,

    /// <summary>
    /// Range check passed, UI is accessible.
    /// </summary>
    Pass,

    /// <summary>
    /// Range check failed, UI is inaccessible.
    /// </summary>
    Fail
}
