using System;
using System.Runtime;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.Commands;

internal sealed class GcCommand : LocalizedCommands
{
    public override string Command => "gc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            GC.Collect();
        }
        else
        {
            if (Parse.TryInt32(args[0], out var generation))
                GC.Collect(generation);
            else
                shell.WriteError(Loc.GetString("cmd-gc-failed-parse"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-gc-arg-generation"));

        return CompletionResult.Empty;
    }
}

internal sealed class GcFullCommand : LocalizedCommands
{
    public override string Command => "gcf";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
    }
}

internal sealed class GcModeCommand : LocalizedCommands
{
    public override string Command => "gc_mode";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var prevMode = GCSettings.LatencyMode;
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-gc_mode-current", ("prevMode", prevMode.ToString())));
            shell.WriteLine(Loc.GetString("cmd-gc_mode-possible"));

            foreach (var mode in Enum.GetValues<GCLatencyMode>())
            {
                shell.WriteLine(Loc.GetString("cmd-gc_mode-option", ("mode", mode.ToString())));
            }
        }
        else
        {
            if (!Enum.TryParse(args[0], true, out GCLatencyMode mode))
            {
                shell.WriteLine(Loc.GetString("cmd-gc_mode-unknown", ("arg", args[0])));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-gc_mode-attempt", ("prevMode", prevMode.ToString()),
                ("mode", mode.ToString())));
            GCSettings.LatencyMode = mode;
            shell.WriteLine(Loc.GetString("cmd-gc_mode-result", ("mode", GCSettings.LatencyMode.ToString())));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                Enum.GetNames<GCLatencyMode>(),
                Loc.GetString("cmd-gc_mode-arg-type"));
        }

        return CompletionResult.Empty;
    }
}

internal sealed class MemCommand : LocalizedCommands
{
    public override string Command => "mem";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var info = GC.GetGCMemoryInfo();

        var heapSize = info.HeapSizeBytes;
        var totalMemory = GC.GetTotalMemory(false);

        shell.WriteLine(Loc.GetString("cmd-mem-report", ("heapSize", heapSize), ("totalAllocated", totalMemory)));
    }
}
