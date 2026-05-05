using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.UserInterface.Stylesheets;

public sealed class DefaultStylesheet
{
    public Stylesheet Stylesheet { get; private set; } = default!;

    public DefaultStylesheet(IResourceCache res, IUserInterfaceManager userInterfaceManager)
    {
        var notoSansFont = res.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf");
        var notoSansFont12 = new VectorFont(notoSansFont, 12);
        var notoSansMonoFont = res.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf");
        var notoSansMono12 = new VectorFont(notoSansMonoFont, 12);


        var theme = userInterfaceManager.CurrentTheme;

        var scrollBarNormal = new StyleBoxFlat {
            BackgroundColor = theme.ResolveColorOrSpecified("scrollBarDefault",Color.FromHex("#80808059")),
            ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };

        var scrollBarHovered = new StyleBoxFlat {
            BackgroundColor = theme.ResolveColorOrSpecified("scrollBarHovered",Color.FromHex("#8C8C8C59")),
            ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };

        var scrollBarGrabbed = new StyleBoxFlat {
            BackgroundColor = theme.ResolveColorOrSpecified("scrollBarGrabbed",Color.FromHex("#8C8C8C59")),
            ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10,
        };

        Stylesheet = new Stylesheet(new StyleRule[]
        {
            /*
             * Debug console and other monospace things.
             */

            Element().Class("monospace")
                .Prop("font", notoSansMono12),

            /*
             * OS Window defaults
             */

            Element<WindowRoot>()
                .Prop("background", theme.ResolveColorOrSpecified("rootBackground", Color.Black)),

            /*
             * Scrollbars
             */

            // VScrollBar grabber normal
            Element<VScrollBar>()
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarNormal),

            // VScrollBar grabber hovered
            Element<VScrollBar>().Pseudo(ScrollBar.StylePseudoClassHover)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarHovered),

            // VScrollBar grabber grabbed
            Element<VScrollBar>().Pseudo(ScrollBar.StylePseudoClassGrabbed)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarGrabbed),

            // HScrollBar grabber normal
            Element<HScrollBar>()
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarNormal),

            // HScrollBar grabber hovered
            Element<HScrollBar>().Pseudo(ScrollBar.StylePseudoClassHover)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarHovered),

            // HScrollBar grabber grabbed
            Element<HScrollBar>().Pseudo(ScrollBar.StylePseudoClassGrabbed)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarGrabbed),

            /*
             * UI Window Defaults
             */

            // Background
            Element().Class(DefaultWindow.StyleClassWindowPanel)
                .Prop("panel", new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("windowBackground", Color.FromHex("#111111")),
                    BorderColor = theme.ResolveColorOrSpecified("windowBorder", Color.FromHex("#444444")),
                    BorderThickness = new Thickness(1),
                }),

            // Header
            Element().Class(DefaultWindow.StyleClassWindowHeader)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat {
                    BackgroundColor = theme.ResolveColorOrSpecified("windowHeader",Color.FromHex("#636396")),
                    BorderColor = theme.ResolveColorOrSpecified("windowBorder", Color.FromHex("#444444")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(1),
                }),

            /*
             * Window close button
             */

            Element().Class(DefaultWindow.StyleClassWindowCloseButton)
                // Button texture
                .Prop(TextureButton.StylePropertyTexture, theme.ResolveTexture("cross"))
                // Normal button color
                .Prop(Control.StylePropertyModulateSelf, theme.ResolveColorOrSpecified("windowCloseButton", Color.FromHex("#FFFFFF"))),

            // Close button hover coloring.
            Element().Class(DefaultWindow.StyleClassWindowCloseButton).Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, theme.ResolveColorOrSpecified("windowCloseButtonHover", Color.FromHex("#FF7F7F"))),

            Element().Class(DefaultWindow.StyleClassWindowCloseButton).Pseudo(TextureButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, theme.ResolveColorOrSpecified("windowCloseButtonPressed", Color.FromHex("#FF0000"))),

            /*
             * Font defaults
             */

            Element()
                .Prop("font", notoSansFont12)
                .Prop("font-color", Color.White),

            /*
             * Buttons
             */

            // Button style normal
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("buttonBackground", Color.FromHex("#171717")),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("buttonBorder", Color.FromHex("#444444")),
                    Padding = new Thickness(3),
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                }),

            // Button style hovered
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("buttonBackgroundHovered", Color.FromHex("#272727")),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("buttonBorderHovered", Color.FromHex("#444444")),
                    Padding = new Thickness(3),
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                }),

            // Button style pressed
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("buttonBackgroundPressed", Color.FromHex("#173717")),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("buttonBorderPressed", Color.FromHex("#447044")),
                    Padding = new Thickness(3),
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                }),

            // Button style disabled
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("buttonBackgroundDisabled", Color.FromHex("#333333")),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("buttonBorderDisabled", Color.FromHex("#222222")),
                    Padding = new Thickness(3),
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                }),

            /*
             * Checkbox
             */

            // CheckBox unchecked
            Element<TextureRect>().Class(CheckBox.StyleClassCheckBox)
                .Prop(TextureRect.StylePropertyTexture, Texture.Black), // TODO: Add actual texture instead of this.

            // CheckBox unchecked
            Element<TextureRect>().Class(CheckBox.StyleClassCheckBox, CheckBox.StyleClassCheckBoxChecked)
                .Prop(TextureRect.StylePropertyTexture, Texture.White), // TODO: Add actual texture instead of this.

            /*
             * LineEdit
             */

            Element<LineEdit>()
                // background color
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("lineEditBackground", Color.FromHex("#000000")),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("lineEditBorder", Color.FromHex("#444444")),
                    Padding = new Thickness(3),
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                })
                // default font color
                .Prop("font-color", Color.White)
                .Prop("cursor-color", Color.White),

            // LineEdit non-editable text color
            Element<LineEdit>().Class(LineEdit.StyleClassLineEditNotEditable)
                .Prop("font-color", theme.ResolveColorOrSpecified("lineEditUneditableText", Color.FromHex("#444444"))),

            // LineEdit placeholder text color
            Element<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color",  theme.ResolveColorOrSpecified("lineEditPlaceholderText", Color.FromHex("#7d7d7d"))),

            /*
             * TabContainer
             */

            Element<TabContainer>()
                // Panel style
                .Prop(TabContainer.StylePropertyPanelStyleBox, new StyleBoxFlat
                {
                    BackgroundColor = theme.ResolveColorOrSpecified("tabContainerBackground", Color.Black),
                    BorderThickness = new Thickness(1),
                    BorderColor = theme.ResolveColorOrSpecified("tabContainerBorder", Color.FromHex("#444444")),
                })
                // Active tab style
                .Prop(TabContainer.StylePropertyTabStyleBox, new StyleBoxFlat {
                    BackgroundColor = theme.ResolveColorOrSpecified("tabContainerActiveTabBackground", Color.FromHex("#173717")),
                    BorderColor = theme.ResolveColorOrSpecified("tabContainerActiveTabBorder", Color.FromHex("#447044")),
                    BorderThickness = new Thickness(1, 1, 1, 0),
                    PaddingLeft = 1,
                    PaddingRight = 1,
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                })
                // Inactive tab style
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, new StyleBoxFlat {
                    BackgroundColor = theme.ResolveColorOrSpecified("tabContainerInactiveTabBackground", Color.FromHex("#173717")),
                    BorderColor = theme.ResolveColorOrSpecified("tabContainerInactiveTabBorder", Color.FromHex("#447044")),
                    BorderThickness = new Thickness(1, 1, 1, 0),
                    PaddingLeft = 1,
                    PaddingRight = 1,
                    ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 5,
                    ContentMarginRightOverride = 5,
                    ContentMarginTopOverride = 3,
                })
                .Prop("font", notoSansFont12),
        });
    }
}
