using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls
{
    internal sealed class DebugVersionPanel : PanelContainer
    {
        public DebugVersionPanel(IConfigurationManager cfg)
        {
            var contents = new Label
            {
                FontColorShadowOverride = Color.Black,
            };
            AddChild(contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(35, 134, 37, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginRightOverride = 5,
                ContentMarginTopOverride = 5,
                ContentMarginBottomOverride = 5,
            };

            MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;

            // Set visible explicitly
            Visible = true;
            HorizontalAlignment = HAlignment.Left;
            VerticalAlignment = VAlignment.Top;

            contents.Text = string.Join('\n', VersionInformationPrinter.GetInformationDump(cfg));
        }
    }
}
