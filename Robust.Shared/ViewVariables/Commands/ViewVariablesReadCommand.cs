using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesReadCommand : ViewVariablesBaseCommand, IConsoleCommand
{
    public override string Command => "vvread";
    public override string Description => "Retrieve a path's value using VV.";
    public override string Help => $"{Command} <path>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments!");
            return;
        }

        var path = args[0];

        if (_netMan.IsClient)
        {
            if (!path.StartsWith("/c"))
            {
                shell.WriteLine(await _vvm.ReadRemotePath(path) ?? "null");
                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        // TODO: Maybe serialize this with serv3 before printing?
        var obj = _vvm.ReadPath(path);
        shell.WriteLine(obj?.ToString() ?? "null");
    }
}
