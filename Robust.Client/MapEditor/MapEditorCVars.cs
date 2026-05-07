using Robust.Shared.Configuration;

namespace Robust.Client.MapEditor;

/// <summary>
/// CVars specific to the map editor.
/// </summary>
[CVarDefs]
internal static class MapEditorCVars
{
    /// <summary>
    /// Maximum amount of tool entries that can be stored in history.
    /// </summary>
    public static readonly CVarDef<int> MaxToolHistory =
        CVarDef.Create("map_editor.max_tool_history", 50, CVar.CLIENTONLY | CVar.ARCHIVE);
}
