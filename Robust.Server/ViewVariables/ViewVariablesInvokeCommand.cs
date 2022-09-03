using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables;

public sealed class ViewVariablesInvokeCommand : IConsoleCommand
{
    public string Command => "vvinvoke";
    public string Description => "Call a method with arguments";
    public string Help => "interior crocodile alligator I drive a chevrolet movie theater";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var path = args[0];
        var arguments = string.Join(string.Empty, args[1..]);

        var vvm = IoCManager.Resolve<IViewVariablesManager>();

        shell.WriteLine(vvm.InvokePath(path, arguments)?.ToString() ?? "null");
    }
}
