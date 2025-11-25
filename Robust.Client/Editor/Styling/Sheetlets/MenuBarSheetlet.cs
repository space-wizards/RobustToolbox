using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class MenuBarSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        return
        [
            Element<MenuBar>()
                .Prop(nameof(Control.Margin), new Thickness(4, 2))
        ];
    }
}
