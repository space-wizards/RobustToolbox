using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesReadCommand : ViewVariablesBaseCommand
{
    public const string Comm = "vvread";

    public override string Command => Comm;

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

        var obj = _vvm.ReadPathSerialized(path);
        shell.WriteLine(obj ?? "null");
    }
}
