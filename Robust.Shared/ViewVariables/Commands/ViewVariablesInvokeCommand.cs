using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Shared.ViewVariables.Commands;

public sealed class ViewVariablesInvokeCommand : ViewVariablesBaseCommand
{
    public override string Command => "vvinvoke";

    protected override VVAccess RequiredAccess => VVAccess.Execute;

    public async override void Execute(IConsoleShell shell, string argStr, string[] args)
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
                var res = await _vvm.InvokeRemotePath(path, arguments);
                if (res.Item2 == ViewVariablesResponseCode.Ok)
                    shell.WriteLine(res.Item1 ?? "null");
                else
                    shell.WriteError($"Failed to invoke. Error code: {res.Item2}. Error: {res.Item1}");
                return;
            }

            // Remove "/c"
            path = path[2..];
        }

        if (_vvm.TryInvokePath(path, arguments, out var obj, out var error))
            shell.WriteLine(obj?.ToString() ?? "null");
        else
            shell.WriteError($"Failed to invoke. Error: {error}");
    }
}
