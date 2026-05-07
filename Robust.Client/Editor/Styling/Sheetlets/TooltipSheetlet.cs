using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class TooltipSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var styleBoxFlat = new StyleBoxFlat
        {
            BackgroundColor = sheet.LowBackground,
            BorderColor = sheet.BaseAccent,
            BorderThickness = new Thickness(1),
        };
        styleBoxFlat.SetContentMarginOverride(StyleBox.Margin.All, 4);

        return
        [
            Element<Tooltip>()
                .Prop(PanelContainer.StylePropertyPanel, styleBoxFlat)
        ];
    }
}
