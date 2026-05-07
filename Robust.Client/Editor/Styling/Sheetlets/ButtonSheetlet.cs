using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class ButtonSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var box = CreateBox(sheet);
        var selectedBox = new StyleBoxTexture(box) { Modulate = sheet.ButtonBackgroundHover };

        var toolButton = new StyleBoxTexture(box) { Modulate = Color.Transparent };

        return
        [
            // Normal buttons
            Element<ContainerButton>()
                .Prop(ContainerButton.StylePropertyStyleBox, box),
            Element<ContainerButton>().Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, selectedBox),

            // Tool buttons
            Element<ContainerButton>()
                .Class(BaseEditorStylesheet.StyleClassToolButton)
                .Prop(ContainerButton.StylePropertyStyleBox, toolButton),
            Element<ContainerButton>().Pseudo(ContainerButton.StylePseudoClassHover)
                .Class(BaseEditorStylesheet.StyleClassToolButton)
                .Prop(ContainerButton.StylePropertyStyleBox, selectedBox),
        ];
    }

    internal static StyleBoxTexture CreateBox(BaseEditorStylesheet sheet)
    {
        var boxTex =
            sheet.Resources.GetResource<TextureResource>("/Engine/Editor/Interface/Controls/button.svg.192dpi.png");

        var box = new StyleBoxTexture
        {
            Texture = boxTex,
            Modulate = sheet.ButtonBackground,
            TextureScale = new Vector2(0.5f, 0.5f),
        };
        box.SetPatchMargin(StyleBox.Margin.All, 8);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);

        return box;
    }
}
