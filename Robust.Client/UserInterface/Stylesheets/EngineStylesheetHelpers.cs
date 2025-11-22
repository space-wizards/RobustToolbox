using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.UserInterface.Stylesheets;

internal static class EngineStylesheetHelpers
{
    public static MutableSelectorChild ParentOf(this MutableSelector selector, MutableSelector other)
    {
        return Child().Parent(selector).Child(other);
    }
}
