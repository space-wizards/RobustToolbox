using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Client.MapEditor;

[RegisterComponent]
[Access(typeof(ClientMapEditorSystem))]
internal sealed partial class MapEditorClientEyeComponent : Component
{
    [ViewVariables]
    public Vector2 Position;

    [ViewVariables]
    public bool PositionDirty;
}

