using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Robust.Shared.MapEditor;

/// <summary>
/// Global state for the map editor. Stored in a singleton component that is networked to all editing clients.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorGlobalStateComponent : Component
{
    [AutoNetworkedField]
    public List<EntityUid> Users = [];

    [AutoNetworkedField]
    public List<EntityUid> Maps = [];
}

/// <summary>
/// Per-user state for the map editor. Also networked to all editing clients.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
internal sealed partial class MapEditorUserStateComponent : Component
{
    [AutoNetworkedField]
    public NetUserId User;

    [AutoNetworkedField]
    public List<EntityUid> OpenMaps = [];

    [AutoNetworkedField]
    public List<EntityUid> Eyes = [];
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorMapDataComponent : Component
{
    [AutoNetworkedField]
    public EntityUid MapEntity;

    [AutoNetworkedField]
    public Dictionary<NetUserId, MapFileHandle> FileHandles = [];
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorEyeComponent : Component
{
    [AutoNetworkedField]
    public EntityUid User;

    [AutoNetworkedField]
    public MapEditorMessages.ActionId Action;

    [AutoNetworkedField]
    public EntityUid MapData;
}
