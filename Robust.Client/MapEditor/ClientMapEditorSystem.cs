using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.MapEditor;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;

namespace Robust.Client.MapEditor;

internal sealed class ClientMapEditorSystem : MapEditorSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IStateManager _stateManager = null!;

    internal event Action<IEnumerable<EntityUid>>? OpenMapsUpdated;
    internal event Action<Entity<MapEditorEyeComponent>>? EyeCreated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapEditorUserDataComponent, ComponentStartup>(EditorUserDataStartup);
        SubscribeLocalEvent<MapEditorUserDataComponent, AfterAutoHandleStateEvent>(AfterUserDataState);
        SubscribeLocalEvent<MapEditorEyeComponent, ComponentStartup>(AfterViewStartup);
    }

    private void EditorUserDataStartup(Entity<MapEditorUserDataComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.User != _playerManager.LocalUser)
            return;

        Log.Info("We're mapping, gamers!");

        _stateManager.RequestStateChange<MapEditorState>();
    }

    private void AfterUserDataState(Entity<MapEditorUserDataComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OpenMapsUpdated?.Invoke(ent.Comp.OpenMaps);
    }

    private void AfterViewStartup(Entity<MapEditorEyeComponent> ent, ref ComponentStartup args)
    {
        EyeCreated?.Invoke(ent);
    }

    public void RequestStartEditing()
    {
        RaiseNetworkEvent(new MEM.StartEditing());
    }

    public void RequestNewFile()
    {
        RaiseNetworkEvent(new MEM.CreateNewMap());
    }

    public void RequestCreateView(EntityUid map, Vector2 position, MEM.ActionId actionId)
    {
        RaiseNetworkEvent(new MEM.CreateNewView
        {
            Action = actionId,
            MapData = GetNetEntity(map),
            Position = position,
        });
    }
}
