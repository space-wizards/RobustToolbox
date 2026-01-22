using Robust.Client.Graphics;
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
        var box = ButtonSheetlet.CreateBox(sheet);

        var emptyBox = new StyleBoxTexture(box) { Modulate = default };
        var fullBox = new StyleBoxTexture(box) { Modulate = sheet.ButtonBackground };

        return
        [
            Element<MenuBar>()
                .Prop(nameof(Control.Margin), new Thickness(4, 2))
                .Prop(MenuBar.StylePropPopupBackground, new StyleBoxFlat
                {
                    BackgroundColor = sheet.LowBackground,
                    BorderColor = sheet.BaseAccent,
                    BorderThickness = new Thickness(1)
                })
                .Prop(MenuBar.StylePropButtonSeparation, 20),

            Element().Class(MenuBar.StyleClassMenuBarButton)
                .Prop(ContainerButton.StylePropertyStyleBox, emptyBox)
                .Prop(nameof(Control.Margin), new Thickness(4, 2)),

            Element().Class(MenuBar.StyleClassMenuBarButton).Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, fullBox),

            Element().Class(MenuBar.StyleClassMenuBarSeparator)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                {
                    BackgroundColor = sheet.HighBackground,
                    ContentMarginBottomOverride = 2,
                    Padding = new Thickness(8, 4)
                })
        ];
    }
}
