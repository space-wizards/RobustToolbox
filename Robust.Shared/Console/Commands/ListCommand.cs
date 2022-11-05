using System.Linq;
using System.Text;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class ListCommands : LocalizedCommands
{
    public override string Command => "list";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var filter = "";
        if (args.Length == 1)
            filter = args[0];

        var host = (IConsoleHostInternal)shell.ConsoleHost;

        var builder = new StringBuilder(Loc.GetString("cmd-list-heading"));
        foreach (var command in host.RegisteredCommands.Values
                     .Where(p => p.Command.Contains(filter))
                     .OrderBy(c => c.Command))
        {
            //TODO: Make this actually check permissions.

            var side = host.IsCmdServer(command) ? "S" : "C";
            builder.AppendLine($"{side} {command.Command,-32}{command.Description}");
        }

        var message = builder.ToString().Trim(' ', '\n');
        shell.WriteLine(message);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-list-arg-filter"));

        return CompletionResult.Empty;
    }
}
