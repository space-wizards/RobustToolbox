using System.Linq;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Shared.Console.Commands;

internal sealed class HelpCommand : LocalizedCommands
{
    private static readonly string Gold = Color.Gold.ToHex();
    private static readonly string Aqua = Color.Aqua.ToHex();

    public override string Command => "help";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Not a toolshed command since it doesn't support optional arguments
        ExecuteStatic(shell, argStr, args);
    }

    public static void ExecuteStatic(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteLine($@"
  TOOLSHED
 /.\\\\\\\\
/___\\\\\\\\
|''''|'''''|
| 8  | === |
|_0__|_____|");
                shell.WriteMarkup($@"
For a list of commands, run [color={Gold}]cmd:list[/color].
To search for commands, run [color={Gold}]cmd:list search ""[color={Aqua}]query[/color]""[/color].
For a breakdown of how a string of commands flows, run [color={Gold}]explain [color={Aqua}]commands here[/color][/color].
For help with old console commands, run [color={Gold}]oldhelp[/color].
");
                break;
            case 1:
                var commandName = args[0];
                if (!shell.ConsoleHost.AvailableCommands.TryGetValue(commandName, out var cmd))
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
        return GetCompletionStatic(shell, args);
    }

    public static CompletionResult GetCompletionStatic(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var host = shell.ConsoleHost;
            return CompletionResult.FromHintOptions(
                host.AvailableCommands.Values.OrderBy(c => c.Command).Select(c => new CompletionOption(c.Command, c.Description)).ToArray(),
                Loc.GetString("cmd-help-arg-cmdname"));
        }

        return CompletionResult.Empty;
    }
}
