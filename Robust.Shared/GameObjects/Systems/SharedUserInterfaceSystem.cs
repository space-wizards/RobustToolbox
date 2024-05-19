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
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transforms = default!;

    private EntityQuery<IgnoreUIRangeComponent> _ignoreUIRangeQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<UserInterfaceComponent> _uiQuery;
    private EntityQuery<UserInterfaceUserComponent> _userQuery;

    private ActorRangeCheckJob _rangeJob;

    public override void Initialize()
    {
        base.Initialize();

        _ignoreUIRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _uiQuery = GetEntityQuery<UserInterfaceComponent>();
        _userQuery = GetEntityQuery<UserInterfaceUserComponent>();

        _rangeJob = new()
        {
            System = this,
            XformQuery = _xformQuery,
        };

        SubscribeAllEvent<BoundUIWrapMessage>((msg, args) =>
        {
            if (args.SenderSession.AttachedEntity is not { } player)
                return;

            OnMessageReceived(msg, player);
        });

        SubscribeLocalEvent<UserInterfaceComponent, OpenBoundInterfaceMessage>(OnUserInterfaceOpen);
        SubscribeLocalEvent<UserInterfaceComponent, CloseBoundInterfaceMessage>(OnUserInterfaceClosed);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentStartup>(OnUserInterfaceStartup);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentGetState>(OnUserInterfaceGetState);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentHandleState>(OnUserInterfaceHandleState);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<UserInterfaceUserComponent, ComponentShutdown>(OnActorShutdown);
        SubscribeLocalEvent<UserInterfaceUserComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        SubscribeLocalEvent<UserInterfaceUserComponent, ComponentGetState>(OnActorGetState);
        SubscribeLocalEvent<UserInterfaceUserComponent, ComponentHandleState>(OnActorHandleState);
    }

    /// <summary>
    /// Validates the received message, and then pass it onto systems/components
    /// </summary>
    private void OnMessageReceived(BoundUIWrapMessage msg, EntityUid sender)
    {
        // This is more or less the main BUI method that handles all messages.

        var uid = GetEntity(msg.Entity);

        if (!_uiQuery.TryComp(uid, out var uiComp))
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
             !actors.Contains(sender)))
        {
            Log.Debug($"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {ToPrettyString(sender)}");
            return;
        }

        // verify that the user is allowed to press buttons on this UI:
        // If it's a close message something else might try to cancel it but we want to force it.
        if (msg.Message is not CloseBoundInterfaceMessage && ui.RequireInputValidation)
        {
            var attempt = new BoundUserInterfaceMessageAttempt(sender, uid, msg.UiKey);
            RaiseLocalEvent(attempt);
            if (attempt.Cancelled)
                return;
        }

        // get the wrapped message and populate it with the sender & UI key information.
        var message = msg.Message;
        message.Actor = sender;
        message.Entity = msg.Entity;
        message.UiKey = msg.UiKey;

        if (uiComp.ClientOpenInterfaces.TryGetValue(msg.UiKey, out var cBui))
        {
            cBui.ReceiveMessage(message);
        }

        // Raise as object so the correct type is used.
        RaiseLocalEvent(uid, (object)message, true);
    }

    #region User

    private void OnActorShutdown(Entity<UserInterfaceUserComponent> ent, ref ComponentShutdown args)
    {
        CloseUserUis((ent.Owner, ent.Comp));
    }

    private void OnGetStateAttempt(Entity<UserInterfaceUserComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Cancelled || args.Player?.AttachedEntity != ent.Owner)
            args.Cancelled = true;
    }

    private void OnActorGetState(Entity<UserInterfaceUserComponent> ent, ref ComponentGetState args)
    {
        var interfaces = new Dictionary<NetEntity, List<Enum>>();

        foreach (var (buid, data) in ent.Comp.OpenInterfaces)
        {
            interfaces[GetNetEntity(buid)] = data;
        }

        args.State = new UserInterfaceUserComponentState()
        {
            OpenInterfaces = interfaces,
        };
    }

    private void OnActorHandleState(Entity<UserInterfaceUserComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not UserInterfaceUserComponentState state)
            return;

        // TODO: Allocate less.
        ent.Comp.OpenInterfaces.Clear();

        foreach (var (nent, data) in state.OpenInterfaces)
        {
            var openEnt = EnsureEntity<ActorComponent>(nent, ent.Owner);
            ent.Comp.OpenInterfaces[openEnt] = data;
        }
    }

    #endregion

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_userQuery.TryGetComponent(ev.Entity, out var actor))
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

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        if (!_userQuery.TryGetComponent(ev.Entity, out var actor))
            return;

        // Close BUIs open detachment.
        foreach (var (uid, keys) in actor.OpenInterfaces)
        {
            if (!_uiQuery.TryGetComponent(uid, out var uiComp))
                continue;

            foreach (var key in keys)
            {
                if (!uiComp.ClientOpenInterfaces.Remove(key, out var cBui))
                    continue;

                cBui.Dispose();
            }
        }
    }

    private void OnUserInterfaceClosed(Entity<UserInterfaceComponent> ent, ref CloseBoundInterfaceMessage args)
    {
        // This handles all of the actually closing BUI.
        // This is because CloseUi just relays the event so client sending message to server vs just server
        // go through the same path.

        var actor = args.Actor;

        var actors = ent.Comp.Actors[args.UiKey];
        actors.Remove(actor);

        if (actors.Count == 0)
            ent.Comp.Actors.Remove(args.UiKey);

        Dirty(ent);

        // If the actor is also deleting then don't worry about updating what they have open.
        if (!TerminatingOrDeleted(actor) && _userQuery.TryComp(actor, out var actorComp))
        {
            if (actorComp.OpenInterfaces.TryGetValue(ent.Owner, out var keys))
            {
                keys.Remove(args.UiKey);

                if (keys.Count == 0)
                    actorComp.OpenInterfaces.Remove(ent.Owner);

                Dirty(actor, actorComp);
            }
        }

        // If we're client we want this handled immediately.
        if (ent.Comp.ClientOpenInterfaces.Remove(args.UiKey, out var cBui))
        {
            cBui.Dispose();
        }

        if (ent.Comp.Actors.Count == 0)
            RemCompDeferred<ActiveUserInterfaceComponent>(ent.Owner);

        var ev = new BoundUIClosedEvent(args.UiKey, ent.Owner, args.Actor);
        RaiseLocalEvent(ent.Owner, ev);
    }

    private void OnUserInterfaceOpen(Entity<UserInterfaceComponent> ent, ref OpenBoundInterfaceMessage args)
    {
        // Similar to the close method this handles actually opening a UI, it just gets relayed here
        EnsureComp<ActiveUserInterfaceComponent>(ent.Owner);

        var actor = args.Actor;
        var actorComp = EnsureComp<UserInterfaceUserComponent>(actor);

        // Let state handling open the UI clientside.
        actorComp.OpenInterfaces.GetOrNew(ent.Owner).Add(args.UiKey);
        ent.Comp.Actors.GetOrNew(args.UiKey).Add(actor);
        Dirty(ent);
        Dirty(actor, actorComp);

        var ev = new BoundUIOpenedEvent(args.UiKey, ent.Owner, args.Actor);
        RaiseLocalEvent(ent.Owner, ev);

        // If we're client we want this handled immediately.
        EnsureClientBui(ent, args.UiKey, ent.Comp.Interfaces[args.UiKey]);
    }

    private void OnUserInterfaceStartup(Entity<UserInterfaceComponent> ent, ref ComponentStartup args)
    {
        // PlayerAttachedEvent will catch some of these.
        foreach (var (key, bui) in ent.Comp.ClientOpenInterfaces)
        {
            bui.Open();

            if (ent.Comp.States.TryGetValue(key, out var state))
            {
                bui.UpdateState(state);
            }
        }
    }

    private void OnUserInterfaceShutdown(EntityUid uid, UserInterfaceComponent component, ComponentShutdown args)
    {
        foreach (var bui in component.ClientOpenInterfaces.Values)
        {
            bui.Dispose();
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
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(ent.Comp.Actors, key, out _);

            existing ??= new List<EntityUid>();

            existing.Clear();
            existing.AddRange(EnsureEntityList<UserInterfaceComponent>(actors, ent.Owner));
        }

        foreach (var key in ent.Comp.Actors.Keys)
        {
            if (state.Actors.ContainsKey(key))
                continue;

            toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            ent.Comp.Actors.Remove(key);
        }

        toRemove.Clear();

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

        // Check if the UI is still open, otherwise call close.
        foreach (var (key, bui) in ent.Comp.ClientOpenInterfaces)
        {
            if (ent.Comp.Actors.ContainsKey(key))
                continue;

            bui.Dispose();
            toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            ent.Comp.ClientOpenInterfaces.Remove(key);
        }

        // update any states we have open
        foreach (var (key, buiState) in state.States)
        {
            if (ent.Comp.States.TryGetValue(key, out var existing) &&
                existing.Equals(buiState))
            {
                continue;
            }

            ent.Comp.States[key] = buiState;

            if (!ent.Comp.ClientOpenInterfaces.TryGetValue(key, out var cBui))
                continue;

            cBui.State = buiState;
            cBui.UpdateState(buiState);
        }

        // If UI not open then open it
        var attachedEnt = _player.LocalEntity;

        // If we get the first state for an ent coming in then don't open BUIs yet, just defer it until later.
        var open = ent.Comp.LifeStage > ComponentLifeStage.Added;

        if (attachedEnt != null)
        {
            foreach (var (key, value) in ent.Comp.Interfaces)
            {
                EnsureClientBui(ent, key, value, open);
            }
        }
    }

    /// <summary>
    /// Opens a client's BUI if not already open and applies the state to it.
    /// </summary>
    private void EnsureClientBui(Entity<UserInterfaceComponent> entity, Enum key, InterfaceData data, bool open = true)
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

        // This is just so we don't open while applying UI states.
        if (!open)
            return;

        boundUserInterface.Open();

        if (entity.Comp.States.TryGetValue(key, out var buiState))
        {
            boundUserInterface.State = buiState;
            boundUserInterface.UpdateState(buiState);
        }
    }

    /// <summary>
    /// Yields all the entities + keys currently open by this entity.
    /// </summary>
    public IEnumerable<(EntityUid Entity, Enum Key)> GetActorUis(Entity<UserInterfaceUserComponent?> entity)
    {
        if (!_userQuery.Resolve(entity.Owner, ref entity.Comp, false))
            yield break;

        foreach (var berry in entity.Comp.OpenInterfaces)
        {
            foreach (var key in berry.Value)
            {
                yield return (berry.Key, key);
            }
        }
    }

    /// <summary>
    /// Gets the actors that have the specified key attached to this entity open.
    /// </summary>
    public IEnumerable<EntityUid> GetActors(Entity<UserInterfaceComponent?> entity, Enum key)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) || !entity.Comp.Actors.TryGetValue(key, out var actors))
            yield break;

        foreach (var actorUid in actors)
        {
            yield return actorUid;
        }
    }

    /// <summary>
    /// Closes the attached UI for all entities.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        for (var i = actors.Count - 1; i >= 0; i--)
        {
            var actor = actors[i];
            CloseUi(entity, key, actor);
        }

        DebugTools.Assert(actors.Count == 0);
    }

    /// <summary>
    /// Closes the attached UI only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession? actor, bool predicted = false)
    {
        var actorEnt = actor?.AttachedEntity;

        if (actorEnt == null)
            return;

        CloseUi(entity, key, actorEnt.Value, predicted);
    }

    /// <summary>
    /// Closes the attached Ui only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid? actor, bool predicted = false)
    {
        if (actor == null)
            return;

        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        // Short-circuit if no UI.
        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(actor.Value))
            return;

        // Rely upon the client telling us.
        if (predicted)
        {
            if (_timing.IsFirstTimePredicted)
            {
                // Not guaranteed to open so rely upon the event handling it.
                // Also lets client request it to be opened remotely too.
                EntityManager.RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new CloseBoundInterfaceMessage(), key));
            }
        }
        else
        {
            OnMessageReceived(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new CloseBoundInterfaceMessage(), key), actor.Value);
        }
    }

    /// <summary>
    /// Tries to call OpenUi and return false if it isn't open.
    /// </summary>
    public bool TryOpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor, bool predicted = false)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        OpenUi(entity, key, actor, predicted);

        // Due to the event actually handling the UI open / closed we can't
        if (!entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(actor))
        {
            return false;
        }

        return true;
    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid? actor, bool predicted = false)
    {
        if (actor == null || !_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        // No implementation for that UI key on this ent so short-circuit.
        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        if (entity.Comp.Actors.TryGetValue(key, out var actors) && actors.Contains(actor.Value))
            return;

        if (predicted)
        {
            if (_timing.IsFirstTimePredicted)
            {
                // Not guaranteed to open so rely upon the event handling it.
                // Also lets client request it to be opened remotely too.
                EntityManager.RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new OpenBoundInterfaceMessage(), key));
            }
        }
        else
        {
            OnMessageReceived(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new OpenBoundInterfaceMessage(), key), actor.Value);
        }
    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession actor, bool predicted = false)
    {
        var actorEnt = actor.AttachedEntity;

        if (actorEnt == null)
            return;

        OpenUi(entity, key, actorEnt.Value, predicted);
    }

    /// <summary>
    /// Sets a BUI state and networks it to all clients.
    /// </summary>
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

    /// <summary>
    /// Returns true if this entity has the specified Ui key available, even if not currently open.
    /// </summary>
    public bool HasUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref ui, false))
            return false;

        return ui.Interfaces.ContainsKey(uiKey);
    }

    /// <summary>
    /// Returns true if the specified UI key is open for this entity by anyone.
    /// </summary>
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

    /// <summary>
    /// Raises a BUI message locally (on client or server) without networking it.
    /// </summary>
    [PublicAPI]
    public void RaiseUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        OnMessageReceived(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), message.Actor);
    }

    #region Server messages

    /// <summary>
    /// Sends a BUI message to any actors who have the specified Ui key open.
    /// </summary>
    public void ServerSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        var filter = Filter.Entities(actors.ToArray());
        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), filter);
    }

    /// <summary>
    /// Sends a Bui message to the specified actor only.
    /// </summary>
    public void ServerSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(actor))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), actor);
    }

    /// <summary>
    /// Sends a Bui message to the specified actor only.
    /// </summary>
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

    /// <summary>
    /// Raises a BUI message from the client to the server.
    /// </summary>
    public void ClientSendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        var player = _player.LocalEntity;

        // Don't send it if we're not a valid actor for it just in case.
        if (player == null ||
            !_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) ||
            !entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(player.Value))
        {
            return;
        }

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key));
    }

    /// <summary>
    /// Closes all Uis for the actor.
    /// </summary>
    public void CloseUserUis(Entity<UserInterfaceUserComponent?> actor)
    {
        if (!_userQuery.Resolve(actor.Owner, ref actor.Comp, false))
            return;

        if (actor.Comp.OpenInterfaces.Count == 0)
            return;

        var copied = new Dictionary<EntityUid, List<Enum>>(actor.Comp.OpenInterfaces);
        var enumCopy = new ValueList<Enum>();

        foreach (var (uid, enums) in copied)
        {
            enumCopy.Clear();
            enumCopy.AddRange(enums);

            foreach (var key in enumCopy)
            {
                CloseUi(uid, key, actor.Owner);
            }
        }
    }

    /// <summary>
    /// Closes all Uis for the entity.
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
    /// Closes all Uis for the entity that the specified actor has open.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        foreach (var key in entity.Comp.Interfaces.Keys)
        {
            CloseUi(entity, key, actor);
        }
    }

    /// <summary>
    /// Closes all Uis for the entity that the specified actor has open.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity, ICommonSession actor)
    {
        if (actor.AttachedEntity is not { } attachedEnt || !_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        CloseUis(entity, attachedEnt);
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
    /// Switches between closed and open for a specific client.
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
    public void SendPredictedUiMessage(BoundUserInterface bui, BoundUserInterfaceMessage msg)
    {
        RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), msg, bui.UiKey));
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var query = AllEntityQuery<ActiveUserInterfaceComponent, UserInterfaceComponent>();
        // Run these in parallel because it's expensive.
        _rangeJob.ActorRanges.Clear();

        // Handles closing the BUI if actors move out of range of them.
        while (query.MoveNext(out var uid, out _, out var uiComp))
        {
            foreach (var (key, actors) in uiComp.Actors)
            {
                DebugTools.Assert(actors.Count > 0);
                var data = uiComp.Interfaces[key];

                // Short-circuit
                if (data.InteractionRange <= 0f || actors.Count == 0)
                    continue;

                foreach (var actor in actors)
                {
                    _rangeJob.ActorRanges.Add((uid, key, data, actor, false));
                }
            }
        }

        _parallel.ProcessNow(_rangeJob, _rangeJob.ActorRanges.Count);

        foreach (var data in _rangeJob.ActorRanges)
        {
            var uid = data.Ui;
            var actor = data.Actor;
            var key = data.Key;

            if (data.Result || Deleted(uid) || Deleted(actor) || !_uiQuery.TryComp(uid, out var uiComp))
                continue;

            CloseUi((uid, uiComp), key, actor);
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
        if (!_xformQuery.TryGetComponent(actor, out var actorXform) || actorXform.MapID != uiMap)
            return false;

        if (_ignoreUIRangeQuery.HasComponent(actor))
            return true;

        // Handle pluggable BoundUserInterfaceCheckRangeEvent
        var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(uid, key, data, actor);
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

    /// <summary>
    /// Used for running UI raycast checks in parallel.
    /// </summary>
    private record struct ActorRangeCheckJob() : IParallelRobustJob
    {
        public EntityQuery<TransformComponent> XformQuery;
        public SharedUserInterfaceSystem System;
        public readonly List<(EntityUid Ui, Enum Key, InterfaceData Data, EntityUid Actor, bool Result)> ActorRanges = new();

        public void Execute(int index)
        {
            var data = ActorRanges[index];

            if (!XformQuery.TryComp(data.Ui, out var uiXform))
            {
                data.Result = false;
            }
            else
            {
                data.Result = System.CheckRange(data.Ui, data.Key, data.Data, data.Actor, uiXform.Coordinates, uiXform.MapID);
            }

            ActorRanges[index] = data;
        }
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

    public readonly InterfaceData Data;

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
        InterfaceData data,
        EntityUid actor)
    {
        Target = target;
        UiKey = uiKey;
        Data = data;
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
