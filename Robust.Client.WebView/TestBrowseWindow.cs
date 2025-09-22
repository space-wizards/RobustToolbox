using System.Numerics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;

namespace Robust.Client.WebView;

internal sealed class TestBrowseWindow : DefaultWindow
{
    protected override Vector2 ContentsMinimumSize => new Vector2(640, 480);

    public TestBrowseWindow()
    {
        var wv = new WebViewControl();
        wv.Url = "https://spacestation14.io";

        Contents.AddChild(wv);
    }
}

internal sealed class TestBrowseWindowCommand : LocalizedCommands
{
    public override string Command => "test_browse_window";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        new TestBrowseWindow().Open();
    }
}
