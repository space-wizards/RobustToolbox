using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.MapEditor;

namespace Robust.Client.MapEditor;

internal sealed partial class ClientMapEditorSystem
{
    internal event Action<Entity<MapEditorEyeComponent>>? EyeCreated;

    private void AfterViewStartup(Entity<MapEditorEyeComponent> ent, ref ComponentStartup args)
    {
        EyeCreated?.Invoke(ent);

        var clientEye = EnsureComp<MapEditorClientEyeComponent>(ent);
        clientEye.Position = TransformSystem.GetWorldPosition(ent);
    }

    private void EyeFrameUpdate()
    {
        var query = AllEntityQuery<MapEditorClientEyeComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var eye, out var transform))
        {
            if (eye.PositionDirty)
            {
                eye.PositionDirty = false;
                SendEyeMoveTo(uid, eye.Position);
            }

            // We continuously override the server position for our eyes.
            // This means the client is fully authoritative over its position,
            // which should make the experience not suck when there's lag.
            TransformSystem.SetWorldPosition((uid, transform), eye.Position);
        }
    }

    private void SendEyeMoveTo(EntityUid eye, Vector2 position)
    {
        RaiseNetworkEvent(new MapEditorMessages.EyeMoveTo
        {
            Eye = GetNetEntity(eye),
            Position = position
        });
    }

    public void EyeMoveTo(EntityUid eye, Vector2 position)
    {
        var comp = Comp<MapEditorClientEyeComponent>(eye);

        if (comp.Position != position)
        {
            comp.Position = position;
            comp.PositionDirty = true;
        }
    }
}
