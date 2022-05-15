using System.Linq;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class HelpCommand : IConsoleCommand
{
    public string Command => "help";
    public string Description => Loc.GetString("cmd-help-desc");
    public string Help => Loc.GetString("cmd-help-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteLine(Loc.GetString("cmd-help-no-args"));
                break;

            case 1:
                var commandName = args[0];
                if (!shell.ConsoleHost.RegisteredCommands.TryGetValue(commandName, out var cmd))
                {
                    shell.WriteError(Loc.GetString("cmd-help-unknown", ("command", commandName)));
                    return;
                }

                shell.WriteLine(Loc.GetString("cmd-help-top", ("command", cmd.Command), ("description", cmd.Description)));
                shell.WriteLine(cmd.Help);
                break;

            default:
                shell.WriteError(Loc.GetString("cmd-help-invalid-args"));
                break;
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var host = shell.ConsoleHost;
            return new CompletionResult(host.RegisteredCommands.Keys.OrderBy(c => c).ToArray(), Loc.GetString("cmd-help-arg-cmdname"));
        }

        return CompletionResult.Empty;
    }
}
