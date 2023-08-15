using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesInvokeCommand : ViewVariablesBaseCommand
{
    public const string Comm = "vvinvoke";
    public override string Command => Comm;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments!");
            return;
        }

        var path = args[0];
        var arguments = string.Join(string.Empty, args[1..]);

        if (_netMan.IsClient)
        {
            if (!path.StartsWith("/c"))
            {
                _vvm.InvokeRemotePath(path, arguments);
                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        var obj = _vvm.InvokePath(path, arguments);
        shell.WriteLine(obj?.ToString() ?? "null");
    }
}
