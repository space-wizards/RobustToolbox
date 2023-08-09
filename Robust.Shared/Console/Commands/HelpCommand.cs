using System.Linq;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class HelpCommand : LocalizedCommands
{
    public override string Command => "oldhelp";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteLine(Loc.GetString("cmd-oldhelp-no-args"));
                break;

            case 1:
                var commandName = args[0];
                if (!shell.ConsoleHost.AvailableCommands.TryGetValue(commandName, out var cmd))
                {
                    shell.WriteError(Loc.GetString("cmd-oldhelp-unknown", ("command", commandName)));
                    return;
                }

                shell.WriteLine(Loc.GetString("cmd-oldhelp-top", ("command", cmd.Command),
                    ("description", cmd.Description)));
                shell.WriteLine(cmd.Help);
                break;

            default:
                shell.WriteError(Loc.GetString("cmd-oldhelp-invalid-args"));
                break;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var host = shell.ConsoleHost;
            return CompletionResult.FromHintOptions(
                host.AvailableCommands.Values.OrderBy(c => c.Command).Select(c => new CompletionOption(c.Command, c.Description)).ToArray(),
                Loc.GetString("cmd-oldhelp-arg-cmdname"));
        }

        return CompletionResult.Empty;
    }
}
