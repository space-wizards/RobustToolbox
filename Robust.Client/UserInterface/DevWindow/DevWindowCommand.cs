using JetBrains.Annotations;
using Robust.Client.Editor.Interface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;

namespace Robust.Client.UserInterface;

[UsedImplicitly]
internal sealed class DevWindowCommand : LocalizedCommands
{
    public override string Command => "devwindow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var (window, docker) = EditorWindowDocker.Create();

        docker.AddPanel(new DebugConsoleTab());
        docker.AddPanel(new DevWindowTabUI());
        docker.AddPanel(new DevWindowTabPerf());
        docker.AddPanel(new DevWindowTabTextures());
        docker.AddPanel(new DevWindowTabRenderTargets());

        window.Show();
    }
}
