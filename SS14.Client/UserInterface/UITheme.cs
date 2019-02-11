using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    public abstract class UITheme
    {
        public abstract Font DefaultFont { get; }
        public abstract Font LabelFont { get; }
        public abstract StyleBox PanelPanel { get; }
        public abstract StyleBox ButtonStyleNormal { get; }
        public abstract StyleBox ButtonStylePressed { get; }
        public abstract StyleBox ButtonStyleDisabled { get; }
        public abstract StyleBox ButtonStyleHovered { get; }
        public abstract StyleBox LineEditBox { get; }
    }

    public sealed class UIThemeDefault : UITheme
    {
        public override Font DefaultFont { get; }
        public override Font LabelFont { get; }
        public override StyleBox PanelPanel { get; }
        public override StyleBox ButtonStyleNormal { get; }
        public override StyleBox ButtonStylePressed { get; }
        public override StyleBox ButtonStyleDisabled { get; }
        public override StyleBox ButtonStyleHovered { get; }
        public override StyleBox LineEditBox { get; }

        public UIThemeDefault()
        {
            var res = IoCManager.Resolve<IResourceCache>();
            var calibri = res.GetResource<FontResource>("/Fonts/CALIBRI.TTF");
            DefaultFont = LabelFont = new VectorFont(calibri, 16);

            PanelPanel = new StyleBoxFlat {BackgroundColor = new Color(37, 37, 45)};

            ButtonStyleNormal = new StyleBoxFlat {BackgroundColor = Color.Gray};
            ButtonStyleNormal.SetContentMarginOverride(StyleBox.Margin.All, 5);
            ButtonStylePressed = new StyleBoxFlat {BackgroundColor = new Color(100, 100, 100)};
            ButtonStylePressed.SetContentMarginOverride(StyleBox.Margin.All, 5);
            ButtonStyleDisabled = new StyleBoxFlat {BackgroundColor = new Color(75, 75, 75)};
            ButtonStyleDisabled.SetContentMarginOverride(StyleBox.Margin.All, 5);
            ButtonStyleHovered = new StyleBoxFlat {BackgroundColor = new Color(128, 128, 140)};
            ButtonStyleHovered.SetContentMarginOverride(StyleBox.Margin.All, 5);
            LineEditBox = new StyleBoxFlat {BackgroundColor = Color.Blue};
            LineEditBox.SetContentMarginOverride(StyleBox.Margin.All, 5);
        }
    }

    public sealed class UIThemeDummy : UITheme
    {
        public override Font DefaultFont { get; } = new DummyFont();
        public override Font LabelFont { get; } = new DummyFont();
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyleNormal { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStylePressed { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyleDisabled { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyleHovered { get; } = new StyleBoxFlat();
        public override StyleBox LineEditBox { get; } = new StyleBoxFlat();
    }
}
