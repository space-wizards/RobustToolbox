using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables;

public sealed class ViewVariablesWriteCommand : IConsoleCommand
{
    public string Command => "vvwrite";
    public string Description => "Modifies a ViewVariables path.";
    public string Help => "a";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var path = args[0];
        var value = args[1];

        var vvm = IoCManager.Resolve<IViewVariablesManager>();

        vvm.WritePath(path, value);
    }
}
