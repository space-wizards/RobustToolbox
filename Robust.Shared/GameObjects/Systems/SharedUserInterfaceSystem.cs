using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    /*
     * TODO:
     * Need the external call methods that raise the event as a predicted event(?)
     * Need to handle closing via event
     * Opening gets handled directly
     * Need to be able to call open in a shared context.
     * When changing mob need to close old UIs and open new ones (internally, don't call the event?)
     * All events get raised shared maybe? Like uhh open UI or close UI or interact with it
     * Server messages only get sent to relevant client.
     */

    private EntityQuery<UserInterfaceComponent> _uiQuery;

    public override void Initialize()
    {
        base.Initialize();

        _uiQuery = GetEntityQuery<UserInterfaceComponent>();

        SubscribeAllEvent<PredictedBoundUIWrapMessage>(OnMessageReceived);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentInit>(OnUserInterfaceInit);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        SubscribeLocalEvent<UserInterfaceComponent, AfterAutoHandleStateEvent>(OnUserInterfaceState);
    }

    private void OnUserInterfaceInit(EntityUid uid, UserInterfaceComponent component, ComponentInit args)
    {
        foreach (var prototypeData in component.Interfaces)
        {
            // TODO: Open UI up that's saved.
            throw new NotImplementedException();
        }
    }

    private void OnUserInterfaceShutdown(EntityUid uid, UserInterfaceComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out ActiveUserInterfaceComponent? activeUis))
            return;

        foreach (var bui in component.ClientOpenInterfaces.Values)
        {
            bui.Close();
        }

        component.ClientOpenInterfaces.Clear();
    }

    private void OnUserInterfaceState(Entity<UserInterfaceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // TODO: Needs to Open UIs that aren't currently open.
        // Needs to close UIs that aren't networked anymore.

        throw new NotImplementedException();
    }

    /// <summary>
    /// Closes the attached UI for all entities.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        // TODO: Need an event raised
        foreach ()
    }

    /// <summary>
    /// Closes the attached UI only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;
    }

    /// <summary>
    /// Closes the attached UI only for the specified actor.
    /// </summary>
    public void CloseUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession actor)
    {
        var actorEnt = actor.AttachedEntity;

        if (actorEnt == null)
            return;

        CloseUi(entity, key, actorEnt.Value);
    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, EntityUid actor)
    {

    }

    public void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, ICommonSession actor)
    {
        var actorEnt = actor.AttachedEntity;

        if (actorEnt == null)
            return;

        OpenUi(entity, key, actorEnt.Value);
    }

    public void SetUiState(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceState? state)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Interfaces.ContainsKey(key))
            return;

        ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(entity.Comp.States, key, out var exists);

        if (exists && state?.Equals(stateRef) == true)
            return;

        stateRef = state;
        Dirty(entity);
    }

    public void SendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors))
            return;

        var filter = Filter.Entities(actors.ToArray());
        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), filter);
    }

    public void SendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, EntityUid actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(actor))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), actor);
    }

    public void SendUiMessage(Entity<UserInterfaceComponent?> entity, Enum key, BoundUserInterfaceMessage message, ICommonSession actor)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false) || actor.AttachedEntity is not { } attachedEntity)
            return;

        if (!entity.Comp.Actors.TryGetValue(key, out var actors) || !actors.Contains(attachedEntity))
            return;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(entity.Owner), message, key), actor);
    }

    /// <summary>
    /// Closes all UIs for the entity.
    /// </summary>
    public void CloseUis(Entity<UserInterfaceComponent?> entity)
    {
        if (!_uiQuery.Resolve(entity.Owner, ref entity.Comp, false))
            return;

        entity.Comp.Interfaces.Clear();
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

        if (!ui.Actors.Contains(attachedEntity))
        {
            Log.Debug($"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {session}");
            return;
        }

        // if they want to close the UI, we can go home early.
        if (msg.Message is CloseBoundInterfaceMessage)
        {
            Close(uid, msg.UiKey, attachedEntity);
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
    /// Tries to get the BUI if it's on the entity at all.
    /// </summary>
    public bool TryGetUi(EntityUid uid, Enum uiKey, [NotNullWhen(true)] out PlayerBoundUserInterface? bui, UserInterfaceComponent? ui = null)
    {
        bui = null;

        return _uiQuery.Resolve(uid, ref ui, false) && ui.Interfaces.TryGetValue(uiKey, out bui);
    }

    /// <summary>
    /// Tries to get the BUI if it is currently open.
    /// </summary>
    public bool TryGetOpenUi(EntityUid uid, Enum uiKey, [NotNullWhen(true)] out BoundUserInterface? bui, UserInterfaceComponent? ui = null)
    {
        bui = null;

        return _uiQuery.Resolve(uid, ref ui, false) && ui.ClientOpenInterfaces.TryGetValue(uiKey, out bui);
    }

    /// <summary>
    ///     Switches between closed and open for a specific client.
    /// </summary>
    public virtual bool TryToggleUi(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        ToggleUi(bui, session);
        return true;
    }

    /// <summary>
    ///     Switches between closed and open for a specific client.
    /// </summary>
    public void ToggleUi(PlayerBoundUserInterface bui, ICommonSession session)
    {
        if (bui._subscribedSessions.Contains(session))
            CloseUi(bui, session);
        else
            OpenUi(bui, session);
    }

    public bool TryOpen(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        return OpenUi(bui, session);
    }

    /// <summary>
    ///     Opens this interface for a specific client.
    /// </summary>
    public bool OpenUi(PlayerBoundUserInterface bui, ICommonSession session)
    {
        if (session.Status == SessionStatus.Connecting || session.Status == SessionStatus.Disconnected)
            return false;

        if (!bui._subscribedSessions.Add(session))
            return false;

        OpenInterfaces.GetOrNew(session).Add(bui);
        RaiseLocalEvent(bui.Owner, new BoundUIOpenedEvent(bui.UiKey, bui.Owner, session));
        if (!bui._subscribedSessions.Contains(session))
        {
            // This can happen if Content closed a BUI from inside the event handler.
            // This will already have caused a redundant close event to be sent to the client, but whatever.
            // Just avoid doing the rest to avoid any state corruption shit.
            return false;
        }

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), new OpenBoundInterfaceMessage(), bui.UiKey), session.Channel);

        // Fun fact, clients needs to have BUIs open before they can receive the state.....
        if (bui.LastStateMsg != null)
            RaiseNetworkEvent(bui.LastStateMsg, session.Channel);

        ActivateInterface(bui);
        return true;
    }

    internal bool Close(EntityUid actor, EntityUid uid, Enum uiKey, bool remoteCall = false, UserInterfaceComponent? uiComp = null)
    {
        if (!Resolve(uid, ref uiComp))
            return false;

        if (!uiComp.ClientOpenInterfaces.TryGetValue(uiKey, out var boundUserInterface))
            return false;

        if (!remoteCall)
            SendUiMessage(boundUserInterface, new CloseBoundInterfaceMessage());

        uiComp.ClientOpenInterfaces.Remove(uiKey);
        boundUserInterface.Dispose();

        if (session != null)
            RaiseLocalEvent(uid, new BoundUIClosedEvent(uiKey, uid, session), true);

        return true;
    }

    public bool TryClose(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        return CloseUi(bui, session);
    }

    /// <summary>
    ///     Close this interface for a specific client.
    /// </summary>
    public bool CloseUi(PlayerBoundUserInterface bui, ICommonSession session, ActiveUserInterfaceComponent? activeUis = null)
    {
        if (!bui._subscribedSessions.Remove(session))
            return false;

        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), new CloseBoundInterfaceMessage(), bui.UiKey), session.Channel);
        CloseShared(bui, session, activeUis);
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
            var xformQuery = GetEntityQuery<TransformComponent>();
            var query = AllEntityQuery<ActiveUserInterfaceComponent, TransformComponent>();

            while (query.MoveNext(out var uid, out var activeUis, out var xform))
            {
                foreach (var ui in activeUis.Interfaces)
                {
                    CheckRange(uid, activeUis, ui, xform, xformQuery);

                    if (!ui.StateDirty)
                        continue;

                    ui.StateDirty = false;

                    foreach (var (player, state) in ui.PlayerStateOverrides)
                    {
                        RaiseNetworkEvent(state, player.Channel);
                    }

                    if (ui.LastStateMsg == null)
                        continue;

                    foreach (var session in ui.SubscribedSessions)
                    {
                        if (!ui.PlayerStateOverrides.ContainsKey(session))
                            RaiseNetworkEvent(ui.LastStateMsg, session.Channel);
                    }
                }
            }
        }

        /// <summary>
        ///     Verify that the subscribed clients are still in range of the interface.
        /// </summary>
        private void CheckRange(EntityUid uid, ActiveUserInterfaceComponent activeUis, PlayerBoundUserInterface ui, TransformComponent transform, EntityQuery<TransformComponent> query)
        {
            if (ui.InteractionRange <= 0)
                return;

            // We have to cache the set of sessions because Unsubscribe modifies the original.
            _sessionCache.Clear();
            _sessionCache.AddRange(ui.SubscribedSessions);

            var uiPos = _xformSys.GetWorldPosition(transform, query);
            var uiMap = transform.MapID;

            foreach (var session in _sessionCache)
            {
                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (!query.TryGetComponent(session.AttachedEntity, out var xform))
                    continue;

                if (_ignoreUIRangeQuery.HasComponent(session.AttachedEntity))
                    continue;

                // Handle pluggable BoundUserInterfaceCheckRangeEvent
                var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(uid, ui, session);
                RaiseLocalEvent(uid, ref checkRangeEvent, broadcast: true);
                if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Pass)
                    continue;

                if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Fail)
                {
                    CloseUi(ui, session, activeUis);
                    continue;
                }

                DebugTools.Assert(checkRangeEvent.Result == BoundUserInterfaceRangeResult.Default);

                if (uiMap != xform.MapID)
                {
                    CloseUi(ui, session, activeUis);
                    continue;
                }

                var distanceSquared = (uiPos - _xformSys.GetWorldPosition(xform, query)).LengthSquared();
                if (distanceSquared > ui.InteractionRangeSqrd)
                    CloseUi(ui, session, activeUis);
            }
        }

        #region Get BUI

        public bool HasUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            return ui.Interfaces.ContainsKey(uiKey);
        }

        public PlayerBoundUserInterface GetUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                throw new InvalidOperationException($"Cannot get {typeof(PlayerBoundUserInterface)} from an entity without {typeof(UserInterfaceComponent)}!");

            return ui.Interfaces[uiKey];
        }

        public PlayerBoundUserInterface? GetUiOrNull(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            return TryGetUi(uid, uiKey, out var bui, ui)
                ? bui
                : null;
        }

        /// <summary>
        ///     Return UIs a session has open.
        ///     Null if empty.
        /// </summary>
        public List<PlayerBoundUserInterface>? GetAllUIsForSession(ICommonSession session)
        {
            OpenInterfaces.TryGetValue(session, out var value);
            return value;
        }
        #endregion

        public bool IsUiOpen(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.SubscribedSessions.Count > 0;
        }

        public bool SessionHasOpenUi(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.SubscribedSessions.Contains(session);
        }

        #region SendMessage

        /// <summary>
        ///     Send a BUI message to all connected player sessions.
        /// </summary>
        public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            SendUiMessage(bui, message);
            return true;
        }

        /// <summary>
        ///     Send a BUI message to all connected player sessions.
        /// </summary>
        public void SendUiMessage(PlayerBoundUserInterface bui, BoundUserInterfaceMessage message)
        {
            var msg = new BoundUIWrapMessage(GetNetEntity(bui.Owner), message, bui.UiKey);
            foreach (var session in bui.SubscribedSessions)
            {
                RaiseNetworkEvent(msg, session.Channel);
            }
        }

        /// <summary>
        ///     Send a BUI message to a specific player session.
        /// </summary>
        public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message, ICommonSession session, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return TrySendUiMessage(bui, message, session);
        }

        /// <summary>
        ///     Send a BUI message to a specific player session.
        /// </summary>
        public bool TrySendUiMessage(PlayerBoundUserInterface bui, BoundUserInterfaceMessage message, ICommonSession session)
        {
            if (!bui.SubscribedSessions.Contains(session))
                return false;

            RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), message, bui.UiKey), session.Channel);
            return true;
        }

        #endregion
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
        public readonly ICommonSession Player;

        /// <summary>
        /// The result of the range check.
        /// </summary>
        public BoundUserInterfaceRangeResult Result;

        public BoundUserInterfaceCheckRangeEvent(
            EntityUid target,
            Enum uiKey,
            ICommonSession player)
        {
            Target = target;
            UiKey = uiKey;
            Player = player;
        }
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
