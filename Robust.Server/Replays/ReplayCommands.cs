using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;

namespace Robust.Server.Replays;

internal sealed class ReplayStartCommand : LocalizedCommands
{
    [Dependency] private readonly IServerReplayRecordingManager _replay = default!;

    public override string Command => "replaystart";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.Recording)
        {
            shell.WriteLine(Loc.GetString("cmd-replaystart-error"));
            return;
        }

        if (args.Length == 0)
        {
            _replay.StartRecording();
            shell.WriteLine(Loc.GetString("cmd-replaystart-success"));
            return;
        }

        if (!float.TryParse(args[0], out var minutes))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[0])));
            return;
        }

        _replay.StartRecording(TimeSpan.FromMinutes(minutes));
        shell.WriteLine(Loc.GetString("cmd-replaystart-success"));
    }
}

internal sealed class ReplayStopCommand : LocalizedCommands
{
    [Dependency] private readonly IServerReplayRecordingManager _replay = default!;

    public override string Command => "replaystop";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.Recording)
        {
            _replay.StopRecording();
            shell.WriteLine(Loc.GetString("cmd-replaystop-success"));
        }
        else
            shell.WriteLine(Loc.GetString("cmd-replaystop-error"));
    }
}

internal sealed class ReplayStatsCommand : LocalizedCommands
{
    [Dependency] private readonly IServerReplayRecordingManager _replay = default!;

    public override string Command => "replaystats";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.Recording)
        {
            var (time, tick, size, _) = _replay.GetReplayStats();
            shell.WriteLine(Loc.GetString("cmd-replaystats-result", ("time", time.ToString("F1")), ("ticks", tick), ("size", size.ToString("F1")), ("rate", (size/time).ToString("F2"))));
        }
        else
            shell.WriteLine(Loc.GetString("cmd-replaystop-error"));
    }
}
