using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

public abstract class SharedUserInterfaceSystem : EntitySystem
{
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
    /// Tries to get the BUIs if it's castable to the specified type.
    /// </summary>
    public IEnumerable<T> GetUis<T>(EntityUid uid, UserInterfaceComponent? ui = null)
    {
        if (!_uiQuery.Resolve(uid, ref ui, false))
            yield break;

        foreach (var bui in ui.Interfaces.Values)
        {
            if (bui is not T cast)
                continue;

            yield return cast;
        }
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
    /// Tries to get the open BUIs if it's castable to the specified type.
    /// </summary>
    public IEnumerable<T> GetOpenUis<T>(EntityUid uid, UserInterfaceComponent? ui = null)
    {
        if (!_uiQuery.Resolve(uid, ref ui, false))
            yield break;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is not T cast)
                continue;

            yield return cast;
        }
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

    /*
     * TODO:
     * Need the external call methods that raise the event as a predicted event(?)
     * Need to handle closing via event
     * Opening gets handled directly
     * Need to be able to call open in a shared context.
     * When changing mob need to close old UIs and open new ones (internally, don't call the event?)
     */

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
