using System.Numerics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;

namespace Robust.Client.WebView;

internal sealed class TestBrowseWindow : DefaultWindow
{
    protected override Vector2 ContentsMinimumSize => new Vector2(640, 480);

    public TestBrowseWindow(string url)
    {
        var wv = new WebViewControl();
        wv.Url = url;

        Contents.AddChild(wv);
    }
}

internal sealed class TestBrowseWindowCommand : LocalizedCommands
{
    public override string Command => "test_browse_window";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var url = args.Length > 0 ? args[0] : "https://spacestation14.com";
        new TestBrowseWindow(url).Open();
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<url>");

        return CompletionResult.Empty;
    }
}
