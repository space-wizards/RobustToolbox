using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.MapEditor;

/// <summary>
/// Global state for the map editor. Stored in a singleton component that is networked to all editing clients.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorGlobalStateComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public List<EntityUid> Users = [];

    [ViewVariables]
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
    [ViewVariables]
    [AutoNetworkedField]
    public NetUserId User;

    [ViewVariables]
    [AutoNetworkedField]
    public List<EntityUid> OpenMaps = [];

    [ViewVariables]
    [AutoNetworkedField]
    public List<EntityUid> Eyes = [];
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorMapDataComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid MapEntity;

    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<NetUserId, MapFileHandle> FileHandles = [];
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
internal sealed partial class MapEditorEyeComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid User;

    [ViewVariables]
    [AutoNetworkedField]
    public MapEditorMessages.ActionId Action;

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid MapData;
}
