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
        public abstract Font LabelFont { get; }

        public abstract StyleBox PanelPanel { get; }
    }

    public sealed class UIThemeDefault : UITheme
    {
        public override Font LabelFont { get; }
        public override StyleBox PanelPanel { get; }

        public UIThemeDefault()
        {
            var res = IoCManager.Resolve<IResourceCache>();
            var calibri = res.GetResource<FontResource>("/Fonts/CALIBRI.TTF");
            LabelFont = new VectorFont(calibri, 12);

            PanelPanel = new StyleBoxFlat {BackgroundColor = new Color(37, 37, 45)};
        }
    }
}
