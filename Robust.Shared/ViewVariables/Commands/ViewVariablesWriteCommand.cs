using Robust.Shared.Console;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesWriteCommand : ViewVariablesBaseCommand
{
    public override string Command => "vvwrite";
    protected override VVAccess RequiredAccess => VVAccess.Write;

    public async override void Execute(IConsoleShell shell, string argStr, string[] args)
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
                var res = await _vvm.WriteRemotePath(path, value);
                if (res.Item2 == ViewVariablesResponseCode.Ok)
                    shell.WriteLine(res.Item1 ?? "null");
                else
                    shell.WriteError($"Failed to write. Error code: {res.Item2}. Error: {res.Item1}");

                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        if (_vvm.TryWritePath(path, value, out var error))
            shell.WriteLine("Successfully written.");
        else
            shell.WriteError($"Failed to write. Error: {error}");

    }
}
