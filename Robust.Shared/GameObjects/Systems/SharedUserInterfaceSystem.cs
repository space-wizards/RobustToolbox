using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
    protected readonly Dictionary<ICommonSession, List<PlayerBoundUserInterface>> OpenInterfaces = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<PredictedBoundUIWrapMessage>(OnMessageReceived);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentInit>(OnUserInterfaceInit);
        SubscribeLocalEvent<UserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
    }

    private void OnUserInterfaceInit(EntityUid uid, UserInterfaceComponent component, ComponentInit args)
    {
        component.Interfaces.Clear();

        foreach (var prototypeData in component.InterfaceData)
        {
            component.Interfaces[prototypeData.UiKey] = new PlayerBoundUserInterface(prototypeData, uid);
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
        var uid = GetEntity(msg.Entity);

        if (!TryComp(uid, out UserInterfaceComponent? uiComp) || args.SenderSession is not { } session)
            return;

        if (!uiComp.Interfaces.TryGetValue(msg.UiKey, out var ui))
        {
            Log.Debug($"Got BoundInterfaceMessageWrapMessage for unknown UI key: {msg.UiKey}");
            return;
        }

        if (!ui.SubscribedSessions.Contains(session))
        {
            Log.Debug($"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {session}");
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

        // Raise as object so the correct type is used.
        RaiseLocalEvent(uid, (object)message, true);
    }

    protected void DeactivateInterface(EntityUid entityUid, PlayerBoundUserInterface ui,
        ActiveUserInterfaceComponent? activeUis = null)
    {
        if (!Resolve(entityUid, ref activeUis, false))
            return;

        activeUis.Interfaces.Remove(ui);
        if (activeUis.Interfaces.Count == 0)
            RemCompDeferred(entityUid, activeUis);
    }

    protected virtual void CloseShared(PlayerBoundUserInterface bui, ICommonSession session,
        ActiveUserInterfaceComponent? activeUis = null)
    {
    }

    public bool TryGetUi(EntityUid uid, Enum uiKey, [NotNullWhen(true)] out PlayerBoundUserInterface? bui, UserInterfaceComponent? ui = null)
    {
        bui = null;

        return Resolve(uid, ref ui, false) && ui.Interfaces.TryGetValue(uiKey, out bui);
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

    private void ActivateInterface(PlayerBoundUserInterface ui)
    {
        EnsureComp<ActiveUserInterfaceComponent>(ui.Owner).Interfaces.Add(ui);
    }

    internal bool TryCloseUi(ICommonSession? session, EntityUid uid, Enum uiKey, bool remoteCall = false, UserInterfaceComponent? uiComp = null)
    {
        if (!Resolve(uid, ref uiComp))
            return false;

        if (!uiComp.OpenInterfaces.TryGetValue(uiKey, out var boundUserInterface))
            return false;

        if (!remoteCall)
            SendUiMessage(boundUserInterface, new CloseBoundInterfaceMessage());

        uiComp.OpenInterfaces.Remove(uiKey);
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
}
