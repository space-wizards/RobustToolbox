using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesWriteCommand : ViewVariablesBaseCommand, IConsoleCommand
{
    public override string Command => "vvwrite";
    public override string Description => "Modify a path's value using VV.";
    public override string Help => $"{Command} <path> <value>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is 0 or >2)
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
