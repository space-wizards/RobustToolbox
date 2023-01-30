using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesReadCommand : ViewVariablesBaseCommand
{
    public override string Command => "vvread";

    protected override VVAccess RequiredAccess => VVAccess.ReadOnly;

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
                var res = await _vvm.ReadRemotePath(path);
                if (res.Item2 == ViewVariablesResponseCode.Ok)
                    shell.WriteLine(res.Item1 ?? "null");
                else
                    shell.WriteError($"Failed to read. Error code: {res.Item2}. Error: {res.Item1}");
                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        if (_vvm.TryReadPathSerialized(path, out var obj, out var error))
            shell.WriteLine(obj ?? "null");
        else
            shell.WriteError($"Failed to read. Error: {error}");
    }
}
