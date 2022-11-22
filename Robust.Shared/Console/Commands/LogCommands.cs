using System;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Shared.Console.Commands;

internal sealed class LogSetLevelCommand : LocalizedCommands
{
    [Dependency] private readonly ILogManager _logManager = default!;

    public override string Command => "loglevel";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Invalid argument amount. Expected 2 arguments.");
            return;
        }

        var name = args[0];
        var levelname = args[1];
        LogLevel? level;
        if (levelname == "null")
        {
            level = null;
        }
        else
        {
            if (!Enum.TryParse<LogLevel>(levelname, out var result))
            {
                shell.WriteLine("Failed to parse 2nd argument. Must be one of the values of the LogLevel enum.");
                return;
            }

            level = result;
        }

        Logger.GetSawmill(name).Level = level;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(
                    _logManager.AllSawmills.Select(c => c.Name).OrderBy(c => c),
                    "<sawmill>");
            case 2:
                return CompletionResult.FromHintOptions(
                    Enum.GetNames<LogLevel>(),
                    "<level>");

            default:
                return CompletionResult.Empty;
        }
    }
}

internal sealed class TestLog : LocalizedCommands
{
    [Dependency] private readonly ILogManager _logManager = default!;

    public override string Command => "testlog";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError("Invalid argument amount. Expected 3 arguments.");
            return;
        }

        var name = args[0];
        var levelname = args[1];
        var message = args[2]; // yes this doesn't support spaces idgaf.
        if (!Enum.TryParse<LogLevel>(levelname, out var result))
        {
            shell.WriteLine("Failed to parse 2nd argument. Must be one of the values of the LogLevel enum.");
            return;
        }

        var level = result;

        Logger.LogS(level, name, message);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(
                    _logManager.AllSawmills.Select(c => c.Name).OrderBy(c => c),
                    "<sawmill>");
            case 2:
                return CompletionResult.FromHintOptions(
                    Enum.GetNames<LogLevel>(),
                    "<level>");

            case 3:
                return CompletionResult.FromHint("<message>");

            default:
                return CompletionResult.Empty;
        }
    }
}
