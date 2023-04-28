namespace Robust.Shared.Console.Commands;

internal sealed class EchoCommand : LocalizedCommands
{
    public override string Command => "echo";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine(string.Join(" ", args));
    }
}
