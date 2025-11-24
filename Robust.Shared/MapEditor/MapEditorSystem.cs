using Robust.Shared.GameObjects;

namespace Robust.Shared.MapEditor;

internal abstract class MapEditorSystem : EntitySystem
{
    protected override string SawmillName => "map_editor.system";
}
