using System.Numerics;
using Robust.Client.Editor.Interface;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.Editor.Styling.Sheetlets;

[EngineSheetlet]
internal sealed class EditorTabSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var tex =
            sheet.Resources.GetResource<TextureResource>("/Engine/Editor/Interface/Controls/tab.svg.192dpi.png");
        var texAccent =
            sheet.Resources.GetResource<TextureResource>("/Engine/Editor/Interface/Controls/tab.svg.accent.192dpi.png");

        var notoSansFont = sheet.Resources.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf");
        var notoSansFont20 = new VectorFont(notoSansFont, 20);

        var box = new StyleBoxTexture
        {
            Texture = tex,
            Modulate = sheet.ButtonBackground,
            TextureScale = new Vector2(0.5f, 0.5f),
            PaddingRight = 2,
        };
        box.SetPatchMargin(StyleBox.Margin.All, 14);
        box.SetPatchMargin(StyleBox.Margin.Bottom, 0);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);

        var selectedBox = new StyleBoxTexture(box) { Modulate = sheet.ButtonBackgroundHover };

        var accentBox = new StyleBoxTexture(box)
        {
            Texture = texAccent,
            Modulate = sheet.BaseAccent
        };

        var highlightBox = new StackedStyleBox(accentBox, box);

        return
        [
            Element<ContainerButton>()
                .Class(EditorTab.StyleClassEditorTabButton)
                .Prop(ContainerButton.StylePropertyStyleBox, box),
            Element<ContainerButton>()
                .Class(EditorTab.StyleClassEditorTabButton)
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, selectedBox),
            Element<ContainerButton>()
                .Class(EditorTab.StyleClassEditorTabButton)
                .Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(ContainerButton.StylePropertyStyleBox, highlightBox),

            Element<Label>()
                .Class("EditorTabTitle", BaseEditorStylesheet.StyleClassEditorDockerLarge)
                .Prop("font", notoSansFont20),
        ];
    }
}

[EngineSheetlet]
internal sealed class EditorTabBackgroundSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = sheet.BaseBackground
        };

        return
        [
            Element<PanelContainer>()
                .Class("EditorPanelBackground")
                .Prop(PanelContainer.StylePropertyPanel, box),
            Element<PanelContainer>()
                .Class("EditorPanelBackground", "EditorDockerLarge")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxEmpty())
        ];
    }
}

[EngineSheetlet]
internal sealed class EditorTabBarSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = sheet.HighBackground
        };

        return
        [
            Element<PanelContainer>()
                .Class("EditorTabBar")
                .Prop(PanelContainer.StylePropertyPanel, box)
        ];
    }
}
