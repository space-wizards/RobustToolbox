using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Stylesheets;

namespace Robust.Client.Editor.Styling;

public static class EditorStylesheetExtensions
{
    public static Stylesheet? GetEditorDark(this IEngineStylesheetAccessor accessor)
    {
        return accessor.GetOrNull(EditorDarkStylesheet.Name);
    }
}
