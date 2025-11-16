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
        var boxTex =
            sheet.Resources.GetResource<TextureResource>("/Engine/Editor/Interface/Controls/button.svg.192dpi.png");

        var box = new StyleBoxTexture
        {
            Texture = boxTex,
            Modulate = Color.Black,
            TextureScale = new Vector2(2, 2),
        };
        box.SetPatchMargin(StyleBox.Margin.All, 2);
        box.SetPadding(StyleBox.Margin.All, 2);

        return
        [
            Element<ContainerButton>()
                .Prop(ContainerButton.StylePropertyStyleBox, box)
        ];
    }
}
