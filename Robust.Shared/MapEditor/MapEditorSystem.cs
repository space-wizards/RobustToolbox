using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.MapEditor;

internal abstract class MapEditorSystem : EntitySystem
{
    [Dependency] private protected SharedTransformSystem TransformSystem = null!;

    protected override string SawmillName => "map_editor.system";
}
