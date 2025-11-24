using Robust.Server.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.MapEditor;
using Robust.Shared.Player;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;
using EState = Robust.Shared.GameObjects.Entity<Robust.Shared.MapEditor.MapEditorStateComponent>;

namespace Robust.Server.MapEditor;

internal sealed partial class ServerMapEditorSystem : MapEditorSystem
{
    [Dependency] private readonly IConGroupController _conGroup = null!;
    [Dependency] private readonly MetaDataSystem _metaSys = null!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = null!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = null!;

    private EntityUid? _stateEntity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MEM.StartEditing>(HandleStartEditing);
        SubscribeNetworkEvent<MEM.CreateNewMap>(HandleCreateMap);
        SubscribeNetworkEvent<MEM.CreateNewView>(HandleCreateView);
        SubscribeNetworkEvent<MEM.DestroyView>(HandleDestroyView);

        Subs.PlayerStatusChanged(_playerManager, PlayerStatusChanged);
    }

    private EState GetOrInitState()
    {
        if (_stateEntity is null)
        {
            Log.Debug("Initializing map editor state!");
            _stateEntity = Spawn();
            _metaSys.SetEntityName(_stateEntity.Value, "MapEditorState");
            var comp = AddComp<MapEditorStateComponent>(_stateEntity.Value);
            return (_stateEntity.Value, comp);
        }

        return GetState()!.Value;
    }

    private EState? GetState()
    {
        if (_stateEntity is null)
            return null;

        return (_stateEntity.Value, Comp<MapEditorStateComponent>(_stateEntity.Value));
    }
}
