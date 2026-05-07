using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.MapEditor;
using Robust.Shared.Player;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using GState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorGlobalStateComponent>;
using UState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorUserStateComponent>;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem
{

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        CheckCleanUpEyes(e);

        if (e.NewStatus != SessionStatus.InGame)
            return;

        var state = GetOrInitState();
        if (GetSessionState(e.Session, state) is not null)
        {
            Log.Debug("Re-enabling session override for reconnected mapping client");
            _pvsOverride.AddSessionOverride(state, e.Session);
        }
    }

    private void HandleStartEditing(MEM.StartEditing msg, EntitySessionEventArgs args)
    {
        if (!_conGroup.CanMapEditor(args.SenderSession))
        {
            Log.Warning($"Client {args.SenderSession} tried to start map editing without required permissions!");
            return;
        }

        var state = GetOrInitState();

        if (GetSessionState(args.SenderSession, state) is not null)
        {
            Log.Debug($"Client {args.SenderSession} tried to start mapping while already mapping. Ignoring.");
            return;
        }

        Log.Info($"Client {args.SenderSession} has started mapping!");

        InitSessionState(args.SenderSession, state);
    }

    private Entity<MapEditorUserStateComponent> InitSessionState(ICommonSession session, GState state)
    {
        _pvsOverride.AddSessionOverride(state, session);

        // Ensure parented so it's accessible via PVS override.
        var userDataEnt = Spawn(null, new EntityCoordinates(state, default));
        var userData = AddComp<MapEditorUserStateComponent>(userDataEnt);
        userData.User = session.UserId;
        MetaSys.SetEntityName(userDataEnt, $"MapEditorUserData {session}");

        state.Comp.Users.Add(userDataEnt);

        Dirty(state);
        Dirty(userDataEnt, userData);

        return (userDataEnt, userData);
    }

    private Entity<MapEditorUserStateComponent>? GetSessionState(ICommonSession session)
    {
        if (GetState() is not { } gState)
            return null;

        return GetSessionState(session, gState);
    }

    private Entity<MapEditorUserStateComponent>? GetSessionState(ICommonSession session, GState state)
    {
        foreach (var user in state.Comp.Users)
        {
            var comp = Comp<MapEditorUserStateComponent>(user);
            if (comp.User == session.UserId)
                return (user, comp);
        }

        return null;
    }

    private bool CommandCheck(
        EntitySessionEventArgs eventArgs,
        EntityEventArgs forCommand,
        out GState globalState,
        out UState userState)
    {
        var session = eventArgs.SenderSession;

        Log.Verbose($"{session}: {forCommand}");

        if (GetState() is not { } gState)
        {
            Log.Warning($"Client {session} tried to execute command {forCommand}, but there's no valid mapping state!");
            globalState = default;
            userState = default;
            return false;
        }

        globalState = gState;

        if (GetSessionState(session, gState) is not { } uState)
        {
            Log.Warning($"Client {session} tried to execute command {forCommand}, but is not mapping!");
            userState = default;
            return false;
        }

        userState = uState;
        return true;
    }

    private delegate void MapCommandSessionHandler<in T>(
        T msg,
        GState gState,
        UState uState,
        EntitySessionEventArgs args);

    private delegate void MapCommandHandler<in T>(
        T msg,
        GState gState,
        UState uState);

    private delegate void MapCommandUserSessionHandler<in T>(
        T msg,
        UState uState,
        EntitySessionEventArgs args);

    private void SubscribeMapCommand<T>(MapCommandSessionHandler<T> handler) where T : EntityEventArgs
    {
        SubscribeNetworkEvent<T>((ev, args) =>
        {
            if (!CommandCheck(args, ev, out var gState, out var uState))
                return;

            handler(ev, gState, uState, args);
        });
    }

    private void SubscribeMapCommand<T>(MapCommandHandler<T> handler) where T : EntityEventArgs
    {
        SubscribeNetworkEvent<T>((ev, args) =>
        {
            if (!CommandCheck(args, ev, out var gState, out var uState))
                return;

            handler(ev, gState, uState);
        });
    }

    private void SubscribeMapCommand<T>(EntitySessionEventHandler<T> handler) where T : EntityEventArgs
    {
        SubscribeNetworkEvent<T>((ev, args) =>
        {
            if (!CommandCheck(args, ev, out _, out _))
                return;

            handler(ev, args);
        });
    }

    private void SubscribeMapCommand<T>(MapCommandUserSessionHandler<T> handler) where T : EntityEventArgs
    {
        SubscribeNetworkEvent<T>((ev, args) =>
        {
            if (!CommandCheck(args, ev, out _, out var uState))
                return;

            handler(ev, uState, args);
        });
    }
}
