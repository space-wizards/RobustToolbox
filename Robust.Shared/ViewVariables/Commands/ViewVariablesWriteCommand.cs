using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesWriteCommand : ViewVariablesBaseCommand
{
    public const string Comm = "vvwrite";
    public override string Command => Comm;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Incorrect number of arguments!");
            return;
        }

        var path = args[0];
        var value = args[1];

        if (_netMan.IsClient)
        {
            if (!path.StartsWith("/c"))
            {
                _vvm.WriteRemotePath(path, value);
                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        _vvm.WritePath(path, value);
    }
}
