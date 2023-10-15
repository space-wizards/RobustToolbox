using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    /// <summary>
    /// Per-tick cache for sessions.
    /// </summary>
    private readonly List<ICommonSession> _sessionCache = new();

    private EntityQuery<IgnoreUIRangeComponent> _ignoreQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _ignoreQuery = GetEntityQuery<IgnoreUIRangeComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        SubscribeAllEvent<PredictedBoundUIWrapMessage>(OnMessageReceived);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentInit>(OnUserInterfaceInit);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);

        SubscribeLocalEvent<ActorUIComponent, ComponentGetState>(OnActorUiGetState);
    }

    private void OnActorUiGetState(EntityUid uid, ActorUIComponent component, ref ComponentGetState args)
    {
        args.State = new ActorUIComponentState(component.OpenBUIS);
    }

    private void OnUserInterfaceInit(EntityUid uid, UserInterfaceComponent component, ComponentInit args)
    {
        component.Interfaces.Clear();
        var netEnt = GetNetEntity(uid);

        foreach (var prototypeData in component.InterfaceData)
        {
            component.Interfaces[prototypeData.UiKey] = new PlayerBoundUserInterface(prototypeData, netEnt);
            component.MappedInterfaceData[prototypeData.UiKey] = prototypeData;
        }
    }

    private void OnUserInterfaceShutdown(EntityUid uid, UserInterfaceComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out ActiveUserInterfaceComponent? activeUis))
            return;

        foreach (var bui in activeUis.Interfaces)
        {
            DeactivateInterface(uid, bui, activeUis);
        }
    }

    /// <summary>
    ///     Validates the received message, and then pass it onto systems/components
    /// </summary>
    internal void OnMessageReceived(BaseBoundUIWrapMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        var uid = GetEntity(msg.Entity);

        if (!TryComp(uid, out UserInterfaceComponent? uiComp))
            return;

        if (!uiComp.Interfaces.TryGetValue(msg.UiKey, out var ui))
        {
            Log.Error($"Got BoundInterfaceMessageWrapMessage for unknown UI key: {msg.UiKey}");
            return;
        }

        if (!ui.SubscribedSessions.Contains(session))
        {
            Log.Debug(
                $"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {session}");
            return;
        }

        // if they want to close the UI, we can go home early.
        if (msg.Message is CloseBoundInterfaceMessage)
        {
            CloseShared(ui, session);
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

        if (msg.Message is OpenBoundInterfaceMessage)
        {
            Open(ui, session);
            return;
        }

        // Raise as object so the correct type is used.
        RaiseLocalEvent(uid, (object)message, true);
    }

    /// <summary>
    /// Removes the UI from active UIs and if relevant removes the active UI component.
    /// </summary>
    private void DeactivateInterface(EntityUid entityUid, PlayerBoundUserInterface ui,
        ActiveUserInterfaceComponent? activeUis = null)
    {
        if (!Resolve(entityUid, ref activeUis, false))
            return;

        activeUis.Interfaces.Remove(ui);
        if (activeUis.Interfaces.Count == 0)
            RemCompDeferred(entityUid, activeUis);
    }

    /// <summary>
    /// Adds the UI to active UIs.
    /// </summary>
    private void ActivateInterface(EntityUid owner, PlayerBoundUserInterface ui)
    {
        EnsureComp<ActiveUserInterfaceComponent>(owner).Interfaces.Add(ui);
    }

    /// <summary>
    /// Tries to close a BUI session.
    /// </summary>
    /// <param name="session">Session trying to close the BUI</param>
    /// <param name="uid">The target BUI</param>
    /// <param name="uiKey"></param>
    /// <param name="uiComp"></param>
    /// <returns></returns>
    internal bool TryClose(ICommonSession? session, EntityUid uid, Enum uiKey, UserInterfaceComponent? uiComp = null)
    {
        if (!Resolve(uid, ref uiComp))
            return false;

        if (!uiComp.OpenInterfaces.TryGetValue(uiKey, out var boundUserInterface))
            return false;

        // Tell server to close please.
        if (_netManager.IsClient && Timing.IsFirstTimePredicted)
            RaisePredictiveEvent(new BoundUIWrapMessage(GetNetEntity(uid), new CloseBoundInterfaceMessage(), uiKey));

        uiComp.OpenInterfaces.Remove(uiKey);
        boundUserInterface.Dispose();

        if (session != null)
            RaiseLocalEvent(uid, new BoundUIClosedEvent(uiKey, uid, session), true);

        return true;
    }

    /// <summary>
    /// Raised by client-side UIs to send to server.
    /// </summary>
    internal void SendUiMessage(BoundUserInterface bui, BoundUserInterfaceMessage msg)
    {
        RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), msg, bui.UiKey));
    }

    /// <summary>
    /// Raised by client-side UIs to send predicted messages to server.
    /// </summary>
    internal void SendPredictedUiMessage(BoundUserInterface bui, BoundUserInterfaceMessage msg)
    {
        RaisePredictiveEvent(new PredictedBoundUIWrapMessage(GetNetEntity(bui.Owner), msg, bui.UiKey));
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
            throw new InvalidOperationException(
                $"Cannot get {typeof(PlayerBoundUserInterface)} from an entity without {typeof(UserInterfaceComponent)}!");

        return ui.Interfaces[uiKey];
    }

    public PlayerBoundUserInterface? GetUiOrNull(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
    {
        return TryGetUi(uid, uiKey, out var bui, ui)
            ? bui
            : null;
    }

    public bool TryGetUi(EntityUid uid, Enum uiKey, [NotNullWhen(true)] out PlayerBoundUserInterface? bui,
        UserInterfaceComponent? ui = null)
    {
        bui = null;

        return Resolve(uid, ref ui, false) && ui.Interfaces.TryGetValue(uiKey, out bui);
    }

    /// <summary>
    ///     Return UIs a session has open.
    ///     Null if empty.
    /// </summary>
    public List<PlayerBoundUserInterface>? GetAllUIsForSession(ICommonSession session)
    {
        if (TryComp<ActorUIComponent>(session.AttachedEntity, out var actorUI))
        {
            return actorUI.OpenBUIS;
        }

        return null;
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

    /// <summary>
    ///     Sets a state. This can be used for stateful UI updating.
    ///     This state is sent to all clients, and automatically sent to all new clients when they open the UI.
    ///     Pretty much how NanoUI did it back in ye olde BYOND.
    /// </summary>
    /// <param name="state">
    ///     The state object that will be sent to all current and future client.
    ///     This can be null.
    /// </param>
    /// <param name="session">
    ///     The player session to send this new state to.
    ///     Set to null for sending it to every subscribed player session.
    /// </param>
    public bool TrySetUiState(EntityUid uid,
        Enum uiKey,
        BoundUserInterfaceState state,
        ICommonSession? session = null,
        UserInterfaceComponent? ui = null,
        bool clearOverrides = true)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        SetUiState(bui, state, session, clearOverrides);
        return true;
    }

    /// <summary>
    ///     Sets a state. This can be used for stateful UI updating.
    ///     This state is sent to all clients, and automatically sent to all new clients when they open the UI.
    ///     Pretty much how NanoUI did it back in ye olde BYOND.
    /// </summary>
    /// <param name="state">
    ///     The state object that will be sent to all current and future client.
    ///     This can be null.
    /// </param>
    /// <param name="session">
    ///     The player session to send this new state to.
    ///     Set to null for sending it to every subscribed player session.
    /// </param>
    public void SetUiState(PlayerBoundUserInterface bui, BoundUserInterfaceState state, ICommonSession? session = null,
        bool clearOverrides = true)
    {
        var msg = new BoundUIWrapMessage(bui.Owner, new UpdateBoundStateMessage(state), bui.UiKey);
        if (session == null)
        {
            bui.StateMessage = msg;
            if (clearOverrides)
                bui.PlayerStateOverrides.Clear();
        }
        else
        {
            bui.PlayerStateOverrides[session] = msg;
        }

        bui.StateDirty = true;
    }

    /// <summary>
    ///     Switches between closed and open for a specific client.
    /// </summary>
    public bool TryToggleUi(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        Toggle(bui, session);
        return true;
    }

    /// <summary>
    ///     Switches between closed and open for a specific client.
    /// </summary>
    public void Toggle(PlayerBoundUserInterface bui, ICommonSession session)
    {
        if (bui._subscribedSessions.Contains(session))
            Close(bui, session);
        else
            Open(bui, session);
    }

    #region Open

    public bool TryOpen(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        if (!Timing.IsFirstTimePredicted)
            return true;

        return Open(bui, session);
    }

    /// <summary>
    ///     Opens this interface for a specific client.
    /// </summary>
    public bool Open(PlayerBoundUserInterface bui, ICommonSession session)
    {
        if (session.Status == SessionStatus.Connecting || session.Status == SessionStatus.Disconnected)
            return false;

        if (!bui._subscribedSessions.Add(session) || session.AttachedEntity == null)
            return false;

        var uiComp = EnsureComp<ActorUIComponent>(session.AttachedEntity.Value);
        uiComp.OpenBUIS.Add(bui);
        var buiEnt = GetEntity(bui.Owner);

        RaiseLocalEvent(buiEnt, new BoundUIOpenedEvent(bui.UiKey, buiEnt, session));
        OpenUiLocal(bui.Owner, bui.UiKey);

        // Fun fact, clients needs to have BUIs open before they can receive the state.....
        if (bui.StateMessage != null)
            RaiseNetworkEvent(bui.StateMessage, session.ConnectedClient);

        ActivateInterface(buiEnt, bui);
        return true;
    }

    protected virtual void OpenUiLocal(NetEntity netEntity, Enum uiKey)
    {
    }

    #endregion

    #region Close

    public void TryClose(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        Close(bui, session);
    }

    /// <summary>
    ///     Close this interface for a specific client.
    /// </summary>
    public bool Close(PlayerBoundUserInterface bui, ICommonSession session,
        ActiveUserInterfaceComponent? activeUis = null)
    {
        if (!bui._subscribedSessions.Remove(session))
            return false;

        CloseShared(bui, session, activeUis);
        return true;
    }

    protected void CloseShared(PlayerBoundUserInterface bui, ICommonSession session,
        ActiveUserInterfaceComponent? activeUis = null)
    {
        var buiEnt = GetEntity(bui.Owner);
        bui._subscribedSessions.Remove(session);
        bui.PlayerStateOverrides.Remove(session);

        if (TryComp(session.AttachedEntity, out ActorUIComponent? actorUi))
        {
            if (actorUi.OpenBUIS.Remove(bui))
            {
                Dirty(session.AttachedEntity.Value, actorUi);
            }
        }

        TryClose(session, buiEnt, bui.UiKey);
        RaiseLocalEvent(buiEnt, new BoundUIClosedEvent(bui.UiKey, buiEnt, session));

        if (bui._subscribedSessions.Count == 0)
            DeactivateInterface(buiEnt, bui, activeUis);
    }

    /// <summary>
    ///     Closes this all interface for any clients that have any open.
    /// </summary>
    public bool TryCloseAll(EntityUid uid, Shared.GameObjects.ActiveUserInterfaceComponent? aui = null)
    {
        if (!Resolve(uid, ref aui, false))
            return false;

        foreach (var ui in aui.Interfaces)
        {
            CloseAll(ui);
        }

        return true;
    }

    /// <summary>
    ///     Closes this specific interface for any clients that have it open.
    /// </summary>
    public bool TryCloseAll(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        CloseAll(bui);
        return true;
    }

    /// <summary>
    ///     Closes this interface for any clients that have it open.
    /// </summary>
    public void CloseAll(PlayerBoundUserInterface bui)
    {
        foreach (var session in bui.SubscribedSessions.ToArray())
        {
            Close(bui, session);
        }
    }

    #endregion

    #region SendMessage

    /// <summary>
    ///     Send a BUI message to all connected player sessions.
    /// </summary>
    public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message,
        UserInterfaceComponent? ui = null)
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
        var msg = new BoundUIWrapMessage(bui.Owner, message, bui.UiKey);
        foreach (var session in bui.SubscribedSessions)
        {
            RaiseNetworkEvent(msg, session.ConnectedClient);
        }
    }

    /// <summary>
    ///     Send a BUI message to a specific player session.
    /// </summary>
    public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message, ICommonSession session,
        UserInterfaceComponent? ui = null)
    {
        if (!TryGetUi(uid, uiKey, out var bui, ui))
            return false;

        return TrySendUiMessage(bui, message, session);
    }

    /// <summary>
    ///     Send a BUI message to a specific player session.
    /// </summary>
    public bool TrySendUiMessage(PlayerBoundUserInterface bui, BoundUserInterfaceMessage message,
        ICommonSession session)
    {
        if (!bui.SubscribedSessions.Contains(session))
            return false;

        RaiseNetworkEvent(new BoundUIWrapMessage(bui.Owner, message, bui.UiKey), session.ConnectedClient);
        return true;
    }

    #endregion

    /// <summary>
    ///     Verify that the subscribed clients are still in range of the interface.
    /// </summary>
    protected void CheckRange(EntityUid uid, ActiveUserInterfaceComponent activeUis, PlayerBoundUserInterface ui,
        TransformComponent transform)
    {
        if (ui.InteractionRange <= 0)
            return;

        // We have to cache the set of sessions because Unsubscribe modifies the original.
        _sessionCache.Clear();
        _sessionCache.AddRange(ui.SubscribedSessions);

        var uiPos = _xformSystem.GetWorldPosition(transform);
        var uiMap = transform.MapID;

        foreach (var session in _sessionCache)
        {
            // The component manages the set of sessions, so this invalid session should be removed soon.
            if (!_xformQuery.TryGetComponent(session.AttachedEntity, out var xform))
                continue;

            if (_ignoreQuery.HasComponent(session.AttachedEntity))
                continue;

            // Handle pluggable BoundUserInterfaceCheckRangeEvent
            var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(uid, ui, session);
            RaiseLocalEvent(uid, ref checkRangeEvent, broadcast: true);
            if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Pass)
                continue;

            if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Fail)
            {
                Close(ui, session, activeUis);
                continue;
            }

            DebugTools.Assert(checkRangeEvent.Result == BoundUserInterfaceRangeResult.Default);

            if (uiMap != xform.MapID)
            {
                Close(ui, session, activeUis);
                continue;
            }

            var distanceSquared = (uiPos - _xformSystem.GetWorldPosition(xform)).LengthSquared();
            if (distanceSquared > ui.InteractionRangeSqrd)
                Close(ui, session, activeUis);
        }
    }
}
