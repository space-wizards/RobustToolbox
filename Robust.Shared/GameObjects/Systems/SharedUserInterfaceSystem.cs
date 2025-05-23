using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    [Dependency] private   readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private   readonly INetManager _netManager = default!;
    [Dependency] private   readonly IParallelManager _parallel = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] private   readonly IReflectionManager _reflection = default!;
    [Dependency] protected readonly ISharedPlayerManager Player = default!;
    [Dependency] private   readonly SharedTransformSystem _transforms = default!;

    private EntityQuery<IgnoreUIRangeComponent> _ignoreUIRangeQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    protected EntityQuery<UserInterfaceComponent> UIQuery;
    protected EntityQuery<UserInterfaceUserComponent> UserQuery;

    private ActorRangeCheckJob _rangeJob;

    /// <summary>
    /// Defer BUIs during state handling so client doesn't spam a BUI constantly during prediction.
    /// </summary>
    private readonly List<(BoundUserInterface Bui, bool value)> _queuedBuis = new();

    public override void Initialize()
    {
        base.Initialize();

        EntityManager.ComponentFactory.RegisterNetworkedFields<UserInterfaceComponent>(
            nameof(UserInterfaceComponent.Actors),
            nameof(UserInterfaceComponent.Interfaces),
            nameof(UserInterfaceComponent.States));

        _ignoreUIRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        UIQuery = GetEntityQuery<UserInterfaceComponent>();
        UserQuery = GetEntityQuery<UserInterfaceUserComponent>();

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
    }

    private void AddQueued(BoundUserInterface bui, bool value)
    {
        _queuedBuis.Add((bui, value));
    }

    /// <summary>
    /// Validates the received message, and then pass it onto systems/components
    /// </summary>
    private void OnMessageReceived(BoundUIWrapMessage msg, EntityUid sender)
    {
        // This is more or less the main BUI method that handles all messages.

        var uid = GetEntity(msg.Entity);

        if (!UIQuery.TryComp(uid, out var uiComp))
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
            var attempt = new BoundUserInterfaceMessageAttempt(sender, uid, msg.UiKey, msg.Message);

            RaiseLocalEvent(attempt);
            if (attempt.Cancelled)
                return;

            RaiseLocalEvent(uid, attempt);
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

    #endregion

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!UserQuery.TryGetComponent(ev.Entity, out var actor))
            return;

        // Open BUIs upon attachment
        foreach (var (uid, keys) in actor.OpenInterfaces)
        {
            if (!UIQuery.TryGetComponent(uid, out var uiComp))
                continue;

            // Player can now receive information about open UIs
            Dirty(uid, uiComp);

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
        if (!UserQuery.TryGetComponent(ev.Entity, out var actor))
            return;

        // Close BUIs open detachment.
        foreach (var (uid, keys) in actor.OpenInterfaces)
        {
            if (!UIQuery.TryGetComponent(uid, out var uiComp))
                continue;

            // Player can no longer receive information about open UIs
            Dirty(uid, uiComp);

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
        CloseUiInternal(ent!, args.UiKey, args.Actor);
    }

    private void CloseUiInternal(Entity<UserInterfaceComponent?> ent, Enum key, EntityUid actor)
    {
        if (!UIQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!ent.Comp.Actors.TryGetValue(key, out var actors))
            return;

        actors.Remove(actor);
        if (actors.Count == 0)
            ent.Comp.Actors.Remove(key);

        DirtyField(ent, nameof(UserInterfaceComponent.Actors));

        // If the actor is also deleting then don't worry about updating what they have open.
        if (!TerminatingOrDeleted(actor)
            && UserQuery.TryComp(actor, out var actorComp)
            && actorComp.OpenInterfaces.TryGetValue(ent.Owner, out var keys))
        {
            keys.Remove(key);
            if (keys.Count == 0)
            {
                actorComp.OpenInterfaces.Remove(ent.Owner);
                if (actorComp.OpenInterfaces.Count == 0)
                    RemCompDeferred<UserInterfaceUserComponent>(actor);
            }
        }

        if (ent.Comp.ClientOpenInterfaces.TryGetValue(key, out var cBui))
        {
            AddQueued(cBui, false);
        }

        if (ent.Comp.Actors.Count == 0)
            RemCompDeferred<ActiveUserInterfaceComponent>(ent.Owner);

        var ev = new BoundUIClosedEvent(key, ent.Owner, actor);
        RaiseLocalEvent(ent.Owner, ev);
    }

    private void OnUserInterfaceOpen(Entity<UserInterfaceComponent> ent, ref OpenBoundInterfaceMessage args)
    {
        OpenUiInternal(ent!, args.UiKey, args.Actor);
    }

    private void OpenUiInternal(Entity<UserInterfaceComponent?> ent, Enum key, EntityUid actor)
    {
        if (!UIQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        // Similar to the close method this handles actually opening a UI, it just gets relayed here
        EnsureComp<ActiveUserInterfaceComponent>(ent.Owner);

        var actorComp = EnsureComp<UserInterfaceUserComponent>(actor);

        // Let state handling open the UI clientside.
        actorComp.OpenInterfaces.GetOrNew(ent.Owner).Add(key);
        ent.Comp.Actors.GetOrNew(key).Add(actor);
        DirtyField(ent, nameof(UserInterfaceComponent.Actors));

        var ev = new BoundUIOpenedEvent(key, ent.Owner, actor);
        RaiseLocalEvent(ent.Owner, ev);

        // If we're client we want this handled immediately.
        EnsureClientBui(ent!, key, ent.Comp.Interfaces[key]);
    }

    private void OnUserInterfaceStartup(Entity<UserInterfaceComponent> ent, ref ComponentStartup args)
    {
        // PlayerAttachedEvent will catch some of these.
        foreach (var (key, bui) in ent.Comp.ClientOpenInterfaces)
        {
            AddQueued(bui, true);
        }
    }

    protected void OnUserInterfaceShutdown(Entity<UserInterfaceComponent> ent, ref ComponentShutdown args)
    {
        var ents = new ValueList<EntityUid>();
        foreach (var (key, acts) in ent.Comp.Actors)
        {
            ents.Clear();
            ents.AddRange(acts);
            foreach (var actor in ents)
            {
                CloseUiInternal(ent!, key, actor);
                DebugTools.Assert(!acts.Contains(actor));
            }

            DebugTools.Assert(!ent.Comp.Actors.ContainsKey(key));
        }

        DebugTools.Assert(ent.Comp.ClientOpenInterfaces.Values.All(x => _queuedBuis.Contains((x, false))));
    }

    private void OnUserInterfaceGetState(Entity<UserInterfaceComponent> ent, ref ComponentGetState args)
    {
        if (ent.Comp.LastFieldUpdate >= args.FromTick)
        {
            var fields = EntityManager.GetModifiedFields(ent.Comp, args.FromTick);

            switch (fields)
            {
                case 1 << 0:
                {
                    var state = new UserInterfaceActorsDeltaState();
                    AddActors(ent, state.Actors, ref args);

                    args.State = state;
                    return;
                }
                case 1 << 2:
                {
                    var states = ent.Comp.States;

                    // TODO Game State
                    // Force the client to serialize & de-serialize implicitly generated component states.
                    if (_netManager.IsClient)
                        states = new(states);

                    args.State = new UserInterfaceStatesDeltaState {States = states};
                    return;
                }
            }
        }

        var actors = new Dictionary<Enum, List<NetEntity>>();
        var dataCopy = new Dictionary<Enum, InterfaceData>(ent.Comp.Interfaces.Count);

        // TODO Game State
        // Force the client to serialize & de-serialize implicitly generated component states.
        foreach (var (weh, a) in ent.Comp.Interfaces)
        {
            dataCopy[weh] = new InterfaceData(a);
        }

        args.State = new UserInterfaceComponentState(actors, new(ent.Comp.States), dataCopy);

        // Ensure that only the player that currently has the UI open gets to know what they have it open.
        AddActors(ent, actors, ref args);
    }

    private void AddActors(Entity<UserInterfaceComponent> ent, Dictionary<Enum, List<NetEntity>> actors, ref ComponentGetState args)
    {
        // Ensure that only the player that currently has the UI open gets to know what they have it open.
        if (args.ReplayState)
        {
            foreach (var (key, acts) in ent.Comp.Actors)
            {
                actors[key] = GetNetEntityList(acts);
            }
        }
        else if (args.Player.AttachedEntity is { } player)
        {
            var netPlayer = new List<NetEntity> { GetNetEntity(player) };
            foreach (var (key, acts) in ent.Comp.Actors)
            {
                if (acts.Contains(player))
                    actors[key] = netPlayer;
            }
        }
    }

    private void OnUserInterfaceHandleState(Entity<UserInterfaceComponent> ent, ref ComponentHandleState args)
    {
        Dictionary<Enum, List<NetEntity>>? stateActors = null;
        Dictionary<Enum, InterfaceData>? stateData = null;
        Dictionary<Enum, BoundUserInterfaceState>? stateStates = null;

        if (args.Current is UserInterfaceComponentState state)
        {
            stateActors = state.Actors;
            stateData = state.Data;
            stateStates = state.States;
        }
        else if (args.Current is UserInterfaceActorsDeltaState actorDelta)
        {
            stateActors = actorDelta.Actors;
        }
        else if (args.Current is UserInterfaceStatesDeltaState stateDelta)
        {
            stateStates = stateDelta.States;
        }
        else
        {
            return;
        }

        // Interfaces
        if (stateData != null)
        {
            ent.Comp.Interfaces.Clear();

            foreach (var data in stateData)
            {
                ent.Comp.Interfaces[data.Key] = new(data.Value);
            }
        }

        var attachedEnt = Player.LocalEntity;

        // Actors
        if (stateActors != null)
        {
            foreach (var key in ent.Comp.Actors.Keys)
            {
                if (!stateActors.ContainsKey(key))
                    CloseUi(ent!, key);
            }

            var toRemoveActors = new ValueList<EntityUid>();
            var newSet = new HashSet<EntityUid>();
            foreach (var (key, acts) in stateActors)
            {
                var actors = ent.Comp.Actors.GetOrNew(key);

                newSet.Clear();
                foreach (var netEntity in acts)
                {
                    var uid = EnsureEntity<UserInterfaceComponent>(netEntity, ent.Owner);
                    if (uid.IsValid())
                        newSet.Add(uid);
                }

                foreach (var actor in newSet)
                {
                    if (!actors.Contains(actor))
                        OpenUiInternal(ent!, key,  actor);
                }

                foreach (var actor in actors)
                {
                    if (!newSet.Contains(actor))
                        toRemoveActors.Add(actor);
                }

                foreach (var actor in toRemoveActors)
                {
                    CloseUiInternal(ent!, key, actor);
                }
            }

            var clientBuis = new ValueList<Enum>(ent.Comp.ClientOpenInterfaces.Keys);

            // Check if the UI is open by us, otherwise dispose of it.
            foreach (var key in clientBuis)
            {
                if (ent.Comp.Actors.TryGetValue(key, out var actors) &&
                    (attachedEnt == null || actors.Contains(attachedEnt.Value)))
                {
                    continue;
                }

                var bui = ent.Comp.ClientOpenInterfaces[key];
                AddQueued(bui, false);
            }
        }

        // States
        if (stateStates != null)
        {
            foreach (var key in ent.Comp.States.Keys)
            {
                if (!stateStates.ContainsKey(key))
                    ent.Comp.States.Remove(key);
            }

            // update any states we have open
            foreach (var (key, buiState) in stateStates)
            {
                if (ent.Comp.States.TryGetValue(key, out var existing) &&
                    existing.Equals(buiState))
                {
                    continue;
                }

                ent.Comp.States[key] = buiState;

                if (!ent.Comp.ClientOpenInterfaces.TryGetValue(key, out var cBui) || !cBui.IsOpened)
                    continue;

                cBui.State = buiState;
                cBui.UpdateState(buiState);
                cBui.Update();
            }
        }

        // If UI not open then open it
        // If we get the first state for an ent coming in then don't open BUIs yet, just defer it until later.
        var open = ent.Comp.LifeStage > ComponentLifeStage.Added;

        if (attachedEnt != null && stateActors != null)
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
        var player = Player.LocalEntity;

        // Existing BUI just keep it.
        if (entity.Comp.ClientOpenInterfaces.TryGetValue(key, out var existing))
        {
            _queuedBuis.Remove((existing, false));
            return;
        }

        if (player == null ||
            !entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(player.Value))
        {
            return;
        }

        DebugTools.Assert(_netManager.IsClient);

        // Try-catch to try prevent error loops / bricked clients that constantly throw exceptions while applying game
        // states. E.g., stripping UI used to throw NREs in some instances while fetching the identity of unknown
        // entities.
        var type = _reflection.LooseGetType(data.ClientType);
        var boundUserInterface = (BoundUserInterface) _factory.CreateInstance(type, [entity.Owner, key]);
        entity.Comp.ClientOpenInterfaces[key] = boundUserInterface;

        // This is just so we don't open while applying UI states.
        if (!open)
            return;

        AddQueued(boundUserInterface, true);
    }

    /// <summary>
    /// Yields all the entities + keys currently open by this entity.
    /// </summary>
    public IEnumerable<(EntityUid Entity, Enum Key)> GetActorUis(Entity<UserInterfaceUserComponent?> entity)
    {
        if (!UserQuery.Resolve(entity.Owner, ref entity.Comp, false))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false) || !entity.Comp.Actors.TryGetValue(key, out var actors))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actorSet))
            return;

        var actors = actorSet.ToArray();
        foreach (var actor in actors)
        {
            CloseUiInternal(entity, key, actor);
        }
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

        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        // Short-circuit if no UI.
        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(actor.Value))
            return;

        // Rely upon the client telling us.
        if (!predicted)
        {
            CloseUiInternal(entity, key, actor.Value);
            return;
        }

        if (!_timing.IsFirstTimePredicted)
            return;

        EntityManager.RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), new CloseBoundInterfaceMessage(), key));
    }

    /// <summary>
    /// Tries to call OpenUi and return false if it isn't open.
    /// </summary>
    public bool TryOpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor, bool predicted = false)
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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

    /// <summary>
    /// Opens the UI for the local client. Does nothing on server.
    /// </summary>
    public virtual void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, bool predicted = false)
    {

    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid? actor, bool predicted = false)
    {
        if (actor == null || !UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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
    /// Tries to return the saved position of a user interface.
    /// </summary>
    public virtual bool TryGetPosition(Entity<UserInterfaceComponent?> entity, Enum key, out Vector2 position)
    {
        position = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// Saves a position for the BUI.
    /// </summary>
    protected virtual void SavePosition(BoundUserInterface bui)
    {

    }

    /// <summary>
    /// Sets a BUI state and networks it to all clients.
    /// </summary>
    public void SetUiState(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceState? state)
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        // Null state
        if (state == null)
        {
            if (!entity.Comp.States.Remove(key))
                return;

            DirtyField(entity, nameof(UserInterfaceComponent.States));
        }
        // Non-null state, check if it matches existing.
        else
        {
            ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(entity.Comp.States, key, out var exists);

            if (exists && stateRef?.Equals(state) == true)
                return;

            stateRef = state;
        }

        // Predict the change on client
        if (state != null && _netManager.IsClient && entity.Comp.ClientOpenInterfaces.TryGetValue(key, out var bui))
        {
            if (bui.State?.Equals(state) != true)
            {
                bui.UpdateState(state);
                bui.Update();
            }
        }

        DirtyField(entity, nameof(UserInterfaceComponent.States));
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.Actors.TryGetValue(uiKey, out var actors))
            return false;

        DebugTools.Assert(actors.Count > 0);
        return actors.Count > 0;
    }

    public bool IsUiOpen(Entity<UserInterfaceComponent?> entity, Enum uiKey, EntityUid actor)
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
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

        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false) || actor.AttachedEntity is not { } attachedEntity)
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
        var player = Player.LocalEntity;

        // Don't send it if we're not a valid actor for it just in case.
        if (player == null ||
            !UIQuery.Resolve(entity.Owner, ref entity.Comp, false) ||
            !entity.Comp.Actors.TryGetValue(key, out var actors) ||
            !actors.Contains(player.Value))
        {
            return;
        }

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key));
    }

    /// <summary>
    /// Closes the user's UIs that match the specified key.
    /// </summary>
    public void CloseUserUis<T>(Entity<UserInterfaceUserComponent?> actor) where T: Enum
    {
        if (!UserQuery.Resolve(actor.Owner, ref actor.Comp, false))
            return;

        if (actor.Comp.OpenInterfaces.Count == 0)
            return;

        var keys = new ValueList<Enum>();
        foreach (var (uid, enums) in actor.Comp.OpenInterfaces)
        {
            keys.Clear();
            keys.AddRange(enums);

            foreach (var weh in keys)
            {
                if (weh is not T)
                    continue;

                CloseUiInternal(uid, weh, actor.Owner);
            }
        }
    }

    /// <summary>
    /// Closes all Uis for the actor.
    /// </summary>
    public void CloseUserUis(Entity<UserInterfaceUserComponent?> actor)
    {
        if (!UserQuery.Resolve(actor.Owner, ref actor.Comp, false))
            return;

        if (actor.Comp.OpenInterfaces.Count == 0)
            return;

        var keys = new ValueList<Enum>();

        foreach (var (uid, enums) in actor.Comp.OpenInterfaces)
        {
            keys.Clear();
            keys.AddRange(enums);

            foreach (var key in keys)
            {
                CloseUiInternal(uid, key, actor.Owner);
            }
        }
    }

    /// <summary>
    /// Closes all Uis for the entity.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity)
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        var toClose = new ValueList<EntityUid>();
        foreach (var (key, actors) in entity.Comp.Actors)
        {
            toClose.Clear();
            toClose.AddRange(actors);
            foreach (var actor in toClose)
            {
                CloseUiInternal(entity, key, actor);
            }
        }
    }

    /// <summary>
    /// Closes all Uis for the entity that the specified actor has open.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity, EntityUid actor)
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        foreach (var key in entity.Comp.Interfaces.Keys)
        {
            CloseUiInternal(entity, key, actor);
        }
    }

    /// <summary>
    /// Closes all Uis for the entity that the specified actor has open.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity, ICommonSession actor)
    {
        if (actor.AttachedEntity is not { } attachedEnt || !UIQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        CloseUis(entity, attachedEnt);
    }

    /// <summary>
    /// Tries to get the BUI if it is currently open.
    /// </summary>
    public bool TryGetOpenUi(Entity<UserInterfaceComponent?> entity, Enum uiKey, [NotNullWhen(true)] out BoundUserInterface? bui)
    {
        bui = null;

        return UIQuery.Resolve(entity.Owner, ref entity.Comp, false) && entity.Comp.ClientOpenInterfaces.TryGetValue(uiKey, out bui);
    }

    /// <summary>
    /// Tries to get the BUI if it is currently open.
    /// </summary>
    public bool TryGetOpenUi<T>(Entity<UserInterfaceComponent?> entity, Enum uiKey, [NotNullWhen(true)] out T? bui) where T : BoundUserInterface
    {
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false) || !entity.Comp.ClientOpenInterfaces.TryGetValue(uiKey, out var cBui))
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
        if (!UIQuery.Resolve(entity.Owner, ref entity.Comp, false) ||
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

    public bool TryGetInterfaceData(Entity<UserInterfaceComponent?> entity,
        Enum key,
        [NotNullWhen(true)] out InterfaceData? data)
    {
        data = null;
        return Resolve(entity, ref entity.Comp, false) && entity.Comp.Interfaces.TryGetValue(key, out data);
    }

    public float GetUiRange(Entity<UserInterfaceComponent?> entity, Enum key)
    {
        TryGetInterfaceData(entity, key, out var data);
        return data?.InteractionRange ?? 0;
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        if (_timing.IsFirstTimePredicted)
        {
            foreach (var (bui, open) in _queuedBuis)
            {
                if (open)
                {
#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    bui.Open();

                    if (UIQuery.TryComp(bui.Owner, out var uiComp))
                    {
                        if (uiComp.States.TryGetValue(bui.UiKey, out var buiState))
                        {
                            bui.State = buiState;
                            bui.UpdateState(buiState);
                            bui.Update();
                        }
                    }
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Caught exception while attempting to create a BUI {bui.UiKey} with type {bui.GetType()} on entity {ToPrettyString(bui.Owner)}. Exception: {e}");
                    }
#endif
                }
                // Close BUI
                else
                {
                    if (UIQuery.TryComp(bui.Owner, out var uiComp))
                    {
                        uiComp.ClientOpenInterfaces.Remove(bui.UiKey);
                    }

#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    if (!TerminatingOrDeleted(bui.Owner))
                    {
                        SavePosition(bui);
                    }

                    bui.Dispose();
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Caught exception while attempting to dispose of a BUI {bui.UiKey} with type {bui.GetType()} on entity {ToPrettyString(bui.Owner)}. Exception: {e}");
                    }
#endif
                }
            }

            _queuedBuis.Clear();
        }

        var query = AllEntityQuery<ActiveUserInterfaceComponent, UserInterfaceComponent>();
        // Run these in parallel because it's expensive.
        _rangeJob.ActorRanges.Clear();

        // Handles closing the BUI if actors move out of range of them.
        // TODO iterate over BUI users, not BUI entities.
        // I.e., a user may have more than one BUI open, but its rare for a bui to be open by more than one user.
        // This means we won't have to fetch the user's transform as frequently.
        while (query.MoveNext(out var uid, out _, out var uiComp))
        {
            foreach (var (key, actors) in uiComp.Actors)
            {
                DebugTools.Assert(actors.Count > 0);
                var data = uiComp.Interfaces[key];

                // Short-circuit
                if (data.InteractionRange <= 0f)
                    continue;

                foreach (var actor in actors)
                {
                    if (_netManager.IsClient && !actor.IsValid())
                        continue; // Client might not have received the entity. Server should log errors.

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

            if (data.Result || Deleted(uid) || Deleted(actor) || !UIQuery.TryComp(uid, out var uiComp))
                continue;

            CloseUi((uid, uiComp), key, actor);
        }
    }

    /// <summary>
    /// Set a UI after an entity has been created.
    /// </summary>
    public void SetUi(Entity<UserInterfaceComponent?> ent, Enum key, InterfaceData data)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            ent.Comp = AddComp<UserInterfaceComponent>(ent);

        ent.Comp.Interfaces[key] = data;
        DirtyField(ent, nameof(UserInterfaceComponent.Interfaces));
    }

    public bool TryGetUiState<T>(Entity<UserInterfaceComponent?> ent, Enum key, [NotNullWhen(true)] out T? state) where T : BoundUserInterfaceState
    {
        if (!Resolve(ent, ref ent.Comp, false) || !ent.Comp.States.TryGetValue(key, out var stateComp))
        {
            state = null;
            return false;
        }

        state = (T)stateComp;
        return true;
    }

    /// <summary>
    ///     Verify that the subscribed clients are still in range of the interface.
    /// </summary>
    private bool CheckRange(
        Entity<TransformComponent> UiEnt,
        Enum key,
        InterfaceData data,
        Entity<TransformComponent> actor)
    {
        if (actor.Comp.MapID != UiEnt.Comp.MapID)
            return false;

        // Handle pluggable BoundUserInterfaceCheckRangeEvent
        var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(UiEnt, key, data, actor!);
        RaiseLocalEvent(UiEnt.Owner, ref checkRangeEvent, true);

        if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Pass)
            return true;

        // We only check if the range check should be ignored if it did not pass.
        // The majority of the time the check will be passing and users generally do not have this component.
        if (_ignoreUIRangeQuery.HasComponent(actor))
            return true;

        if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Fail)
            return false;

        DebugTools.Assert(checkRangeEvent.Result == BoundUserInterfaceRangeResult.Default);

        return _transforms.InRange(UiEnt!, (actor.Owner, actor.Comp), data.InteractionRange);
    }

    /// <summary>
    /// Used for running UI raycast checks in parallel.
    /// </summary>
    private record struct ActorRangeCheckJob() : IParallelRobustJob
    {
        public required EntityQuery<TransformComponent> XformQuery;
        public required SharedUserInterfaceSystem System;
        public readonly List<(EntityUid Ui, Enum Key, InterfaceData Data, EntityUid Actor, bool Result)> ActorRanges = new();

        public void Execute(int index)
        {
            var data = ActorRanges[index];

            if (!XformQuery.TryComp(data.Ui, out var uiXform) ||
                !XformQuery.TryComp(data.Actor, out var actorXform))
            {
                data.Result = false;
            }
            else
            {
                data.Result = System.CheckRange((data.Ui, uiXform), data.Key, data.Data, (data.Actor, actorXform));
            }

            ActorRanges[index] = data;
        }
    }
}

/// <summary>
/// Raised by <see cref="UserInterfaceSystem"/> to check whether an interface is still accessible by its user.
/// The event is raised directed at the entity that owns the interface.
/// </summary>
[ByRefEvent]
[PublicAPI]
public struct BoundUserInterfaceCheckRangeEvent(
    Entity<TransformComponent> target,
    Enum uiKey,
    InterfaceData data,
    Entity<TransformComponent> actor)
{
    /// <summary>
    /// The entity owning the UI being checked for.
    /// </summary>
    public readonly EntityUid Target = target;

    /// <summary>
    /// The UI itself.
    /// </summary>
    /// <returns></returns>
    public readonly Enum UiKey = uiKey;

    public readonly InterfaceData Data = data;

    /// <summary>
    /// The player for which the UI is being checked.
    /// </summary>
    public readonly Entity<TransformComponent> Actor = actor;

    /// <summary>
    /// The result of the range check.
    /// </summary>
    public BoundUserInterfaceRangeResult Result;
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
