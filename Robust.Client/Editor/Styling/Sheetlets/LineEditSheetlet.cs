using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class LineEditSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var boxOuterTex =
            sheet.Resources.GetResource<TextureResource>(
                "/Engine/Editor/Interface/Controls/lineedit.svg.outer.192dpi.png");
        var boxInnerTex =
            sheet.Resources.GetResource<TextureResource>(
                "/Engine/Editor/Interface/Controls/lineedit.svg.inner.192dpi.png");

        var boxOuter = new StyleBoxTexture
        {
            Texture = boxOuterTex,
            TextureScale = new Vector2(0.5f, 0.5f),
            Modulate = sheet.ButtonBackground,
        };
        boxOuter.SetPatchMargin(StyleBox.Margin.All, 8);

        var boxInner = new StyleBoxTexture
        {
            Texture = boxInnerTex,
            TextureScale = new Vector2(0.5f, 0.5f),
            Modulate = sheet.BaseBackground,
        };
        boxInner.SetPatchMargin(StyleBox.Margin.All, 8);

        var stackedBox = new StackedStyleBox(boxOuter, boxInner);

        return
        [
            Element<LineEdit>()
                .Prop(LineEdit.StylePropertyStyleBox, stackedBox)
        ];
    }
}
