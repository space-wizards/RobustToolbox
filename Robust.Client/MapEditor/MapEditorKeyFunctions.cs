using Robust.Shared.Input;

namespace Robust.Client.MapEditor;

[KeyFunctions]
internal static class MapEditorKeyFunctions
{
    // "File" menu
    public static readonly BoundKeyFunction NewFile = "MapEditorNewFile";
    public static readonly BoundKeyFunction OpenFile = "MapEditorOpenFile";
    public static readonly BoundKeyFunction SaveFile = "MapEditorSaveFile";
    public static readonly BoundKeyFunction SaveAsFile = "MapEditorSaveAsFile";
    public static readonly BoundKeyFunction CloseFile = "MapEditorCloseFile";

    // "Edit" menu
    public static readonly BoundKeyFunction Undo = "MapEditorUndo";
    public static readonly BoundKeyFunction Redo = "MapEditorRedo";

    // Viewport
    public static readonly BoundKeyFunction ViewportDrag = "MapEditorViewportDrag";
}
