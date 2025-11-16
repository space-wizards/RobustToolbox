using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Stylesheets;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class ConsoleSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = sheet.BaseBackground with { A = 0.8f },
        };
        box.SetContentMarginOverride(StyleBox.Margin.All, 3);

        return
        [
            Element<PanelContainer>()
                .Class(DropDownDebugConsole.ClassDropDownBackground)
                .Prop(PanelContainer.StylePropertyPanel, box),
        ];
    }
}
