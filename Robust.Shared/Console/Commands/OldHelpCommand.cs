namespace Robust.Shared.Console.Commands;

public sealed class OldHelpCommand : LocalizedCommands
{
    public override string Command => "oldhelp";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        HelpCommand.ExecuteStatic(shell, argStr, args);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return HelpCommand.GetCompletionStatic(shell, args);
    }
}
