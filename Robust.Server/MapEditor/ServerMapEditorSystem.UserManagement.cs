using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.MapEditor;
using Robust.Shared.Player;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using EState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorStateComponent>;

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

    private Entity<MapEditorUserDataComponent> InitSessionState(ICommonSession session, EState state)
    {
        _pvsOverride.AddSessionOverride(state, session);

        // Ensure parented so it's accessible via PVS override.
        var userDataEnt = Spawn(null, new EntityCoordinates(state, default));
        var userData = AddComp<MapEditorUserDataComponent>(userDataEnt);
        userData.User = session.UserId;
        _metaSys.SetEntityName(userDataEnt, $"MapEditorUserData {session}");

        state.Comp.Users.Add(userDataEnt);

        Dirty(state);
        Dirty(userDataEnt, userData);

        return (userDataEnt, userData);
    }

    private Entity<MapEditorUserDataComponent>? GetSessionState(ICommonSession session)
    {
        if (GetState() is not { } gState)
            return null;

        return GetSessionState(session, gState);
    }

    private Entity<MapEditorUserDataComponent>? GetSessionState(ICommonSession session, EState state)
    {
        foreach (var user in state.Comp.Users)
        {
            var comp = Comp<MapEditorUserDataComponent>(user);
            if (comp.User == session.UserId)
                return (user, comp);
        }

        return null;
    }

    private bool CommandCheck(
        EntitySessionEventArgs eventArgs,
        EntityEventArgs forCommand,
        out EState globalState,
        out Entity<MapEditorUserDataComponent> userState)
    {
        var session = eventArgs.SenderSession;
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
}
