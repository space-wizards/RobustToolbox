using Robust.Shared.Input;

namespace Robust.Client.MapEditor;

internal static class MapEditorInputContext
{
    public const string ContextName = "mapEditor";

    public static void SetupContexts(IInputContextContainer contexts)
    {
        var editor = contexts.New(ContextName, InputContextContainer.DefaultContextName);

        editor.AddFunction(MapEditorKeyFunctions.ViewportDrag);
    }
}
