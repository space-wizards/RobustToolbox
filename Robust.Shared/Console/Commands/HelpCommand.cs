using System.Linq;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class HelpCommand : LocalizedCommands
{
    public override string Command => "help";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

                shell.WriteLine(Loc.GetString("cmd-help-top", ("command", cmd.Command),
                    ("description", cmd.Description)));
                shell.WriteLine(cmd.Help);
                break;

            default:
                shell.WriteError(Loc.GetString("cmd-help-invalid-args"));
                break;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var host = shell.ConsoleHost;
            return CompletionResult.FromHintOptions(
                host.RegisteredCommands.Values.OrderBy(c => c.Command).Select(c => new CompletionOption(c.Command, c.Description)).ToArray(),
                Loc.GetString("cmd-help-arg-cmdname"));
        }

        return CompletionResult.Empty;
    }
}
