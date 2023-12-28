using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls;

internal sealed class DebugSystemPanel : PanelContainer
{
    public DebugSystemPanel()
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

        HorizontalAlignment = HAlignment.Left;

        contents.Text = string.Join('\n', RuntimeInformationPrinter.GetInformationDump());
    }
}
