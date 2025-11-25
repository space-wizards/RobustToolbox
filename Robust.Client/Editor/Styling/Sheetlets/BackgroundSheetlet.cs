using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class BackgroundSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = sheet.LowBackground
        };

        return
        [
            Element<PanelContainer>()
                .Class(BaseEditorStylesheet.StyleClassLowBackground)
                .Prop(PanelContainer.StylePropertyPanel, box)
        ];
    }
}
