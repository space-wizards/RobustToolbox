using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.MapEditor;
using Robust.Shared.Player;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem
{
    [Dependency] private readonly SharedViewSubscriberSystem _viewSub = null!;

    private void HandleCreateView(MapEditorMessages.CreateNewView msg, EntitySessionEventArgs args)
    {
        if (!CommandCheck(args, msg, out _, out var uState))
            return;

        var mapEnt = GetEntity(msg.MapData);
        var mapData = Comp<MapEditorMapDataComponent>(mapEnt);

        var eyeEnt = Spawn(null, new EntityCoordinates(mapData.MapEntity, msg.Position));
        _metaSys.SetEntityName(eyeEnt, $"MapEditorEye for {args.SenderSession}");
        AddComp<EyeComponent>(eyeEnt);
        var eyeData = AddComp<MapEditorEyeComponent>(eyeEnt);
        eyeData.User = uState;
        eyeData.Action = msg.Action;
        eyeData.MapData = mapEnt;
        _viewSub.AddViewSubscriber(eyeEnt, args.SenderSession);

        uState.Comp.Eyes.Add(eyeEnt);
        Dirty(uState);

        Log.Debug($"Created view {ToPrettyString(eyeEnt)}");

        Dirty(eyeEnt, eyeData);
    }

    private void HandleDestroyView(MapEditorMessages.DestroyView msg, EntitySessionEventArgs args)
    {
        if (!CommandCheck(args, msg, out _, out var uState))
            return;

        var eyeEnt = GetEntity(msg.Eye);
        var eyeData = Comp<MapEditorEyeComponent>(eyeEnt);
        if (eyeData.User != uState.Owner)
        {
            Log.Warning($"User {args.SenderSession} tried to delete eye owned by {ToPrettyString(uState)}");
            return;
        }

        DestroyEye((eyeEnt, eyeData), uState);
    }

    private void DestroyEye(Entity<MapEditorEyeComponent> eyeEnt, Entity<MapEditorUserDataComponent> uState)
    {
        uState.Comp.Eyes.Remove(eyeEnt);
        Dirty(uState);

        Log.Debug($"Destroying view {ToPrettyString(eyeEnt)}");
        Del(eyeEnt);
    }

    private void CheckCleanUpEyes(SessionStatusEventArgs eventArgs)
    {
        if (eventArgs.NewStatus != SessionStatus.Disconnected)
            return;

        if (GetSessionState(eventArgs.Session) is not { } uState)
            return;

        foreach (var eye in uState.Comp.Eyes.ToArray())
        {
            DestroyEye((eye, Comp<MapEditorEyeComponent>(eye)), uState);
        }
    }
}
