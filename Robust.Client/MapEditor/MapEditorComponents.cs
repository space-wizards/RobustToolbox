using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.MapEditor;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.MapEditor;

/// <summary>
/// Client-side data for <see cref="MapEditorEyeComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(ClientMapEditorSystem))]
internal sealed partial class MapEditorClientEyeComponent : Component
{
    [ViewVariables]
    public Vector2 Position;

    [ViewVariables]
    public bool PositionDirty;
}

/// <summary>
/// Client-side data for <see cref="MapEditorMapDataComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(ClientMapEditorSystem))]
internal sealed partial class MapEditorClientMapDataComponent : Component
{
    [ViewVariables]
    public EntityUid? ActiveToolEntity;

    // Most recent entity first.
    [ViewVariables]
    public List<EntityUid> ToolEntityHistory = [];
}

[RegisterComponent]
[Access(typeof(ClientMapEditorSystem))]
internal sealed partial class MapEditorToolDataComponent : Component
{
    [ViewVariables]
    public FormattedMessage ToolName = new();
}
