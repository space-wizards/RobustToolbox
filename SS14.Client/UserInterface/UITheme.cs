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
    }

    public sealed class UIThemeDefault : UITheme
    {
        public override Font DefaultFont { get; }
        public override Font LabelFont { get; }
        public override StyleBox PanelPanel { get; }
        public override StyleBox ButtonStyleNormal { get; }

        public UIThemeDefault()
        {
            var res = IoCManager.Resolve<IResourceCache>();
            var calibri = res.GetResource<FontResource>("/Fonts/CALIBRI.TTF");
            DefaultFont = LabelFont = new VectorFont(calibri, 16);

            PanelPanel = new StyleBoxFlat {BackgroundColor = new Color(37, 37, 45)};

            ButtonStyleNormal = new StyleBoxFlat {BackgroundColor = Color.Gray};
            ButtonStyleNormal.SetContentMarginOverride(StyleBox.Margin.All, 5);
        }
    }

    public sealed class UIThemeDummy : UITheme
    {
        public override Font DefaultFont { get; } = new DummyFont();
        public override Font LabelFont { get; } = new DummyFont();
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyleNormal { get; } = new StyleBoxFlat();
    }
}
