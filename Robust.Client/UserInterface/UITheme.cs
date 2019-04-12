using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Fallback theme system for GUI.
    /// </summary>
    public abstract class UITheme
    {
        public abstract Font DefaultFont { get; }
        public abstract Font LabelFont { get; }
        public abstract StyleBox PanelPanel { get; }
        public abstract StyleBox ButtonStyle { get; }
        public abstract StyleBox LineEditBox { get; }
    }

    public sealed class UIThemeDefault : UITheme
    {
        public override Font DefaultFont { get; }
        public override Font LabelFont { get; }
        public override StyleBox PanelPanel { get; }
        public override StyleBox ButtonStyle { get; }
        public override StyleBox LineEditBox { get; }

        public UIThemeDefault()
        {
            var res = IoCManager.Resolve<IResourceCache>();
            var calibri = res.GetResource<FontResource>("/Fonts/CALIBRI.TTF");
            DefaultFont = LabelFont = new VectorFont(calibri, 16);

            PanelPanel = new StyleBoxFlat {BackgroundColor = new Color(37, 37, 45)};

            ButtonStyle = new StyleBoxFlat {BackgroundColor = Color.Gray};
            ButtonStyle.SetContentMarginOverride(StyleBox.Margin.All, 5);
            LineEditBox = new StyleBoxFlat {BackgroundColor = Color.Blue};
            LineEditBox.SetContentMarginOverride(StyleBox.Margin.All, 5);
        }
    }

    public sealed class UIThemeDummy : UITheme
    {
        public override Font DefaultFont { get; } = new DummyFont();
        public override Font LabelFont { get; } = new DummyFont();
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyle { get; } = new StyleBoxFlat();
        public override StyleBox LineEditBox { get; } = new StyleBoxFlat();
    }
}
