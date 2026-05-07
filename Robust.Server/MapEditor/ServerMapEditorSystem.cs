using Robust.Server.Console;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.MapEditor;
using Robust.Shared.Player;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using GState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorGlobalStateComponent>;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem : MapEditorSystem
{
    [Dependency] private readonly IConGroupController _conGroup = null!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = null!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = null!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = null!;

    private EntityUid? _stateEntity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MEM.StartEditing>(HandleStartEditing);

        SubscribeMapCommand<MEM.CreateNewMap>(HandleCreateMap);
        SubscribeMapCommand<MEM.OpenMap>(HandleOpenMap);
        SubscribeMapCommand<MEM.CloseMap>(HandleCloseMap);
        SubscribeMapCommand<MEM.SaveMap>(HandleSaveMap);
        SubscribeMapCommand<MEM.CreateNewView>(HandleCreateView);
        SubscribeMapCommand<MEM.DestroyView>(HandleDestroyView);

        Subs.PlayerStatusChanged(_playerManager, PlayerStatusChanged);

        _mapLoader.OnIsSerializable += MapLoaderOnOnIsSerializable;
    }

    private void MapLoaderOnOnIsSerializable(Entity<MetaDataComponent> ent, ref bool serializable)
    {
        if (HasComp<MapEditorUnsavedComponent>(ent))
            serializable = false;
    }

    private GState GetOrInitState()
    {
        if (_stateEntity is null)
        {
            Log.Debug("Initializing map editor state!");
            _stateEntity = Spawn();
            MetaSys.SetEntityName(_stateEntity.Value, "MapEditorState");
            var comp = AddComp<MapEditorGlobalStateComponent>(_stateEntity.Value);
            return (_stateEntity.Value, comp);
        }

        return GetState()!.Value;
    }

    private GState? GetState()
    {
        if (_stateEntity is null)
            return null;

        return (_stateEntity.Value, Comp<MapEditorGlobalStateComponent>(_stateEntity.Value));
    }
}
