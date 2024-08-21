using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;
using Robust.Shared.ContentPack;

namespace Robust.Shared.Replays;

internal sealed class ReplayStartCommand : LocalizedCommands
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

    public override string Command => "replay_recording_start";
    public override string Description => LocalizationManager.GetString($"cmd-replay-recording-start-desc");
    public override string Help => LocalizationManager.GetString($"cmd-replay-recording-start-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.IsRecording)
        {
            shell.WriteError(Loc.GetString("cmd-replay-recording-start-already-recording"));
            return;
        }

        string? dir = args.Length == 0 ? null : args[0];

        var overwrite = false;
        if (args.Length > 1)
        {
            if (!bool.TryParse(args[1], out overwrite))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[2])));
                return;
            }
        }

        TimeSpan? duration = null;
        if (args.Length > 2)
        {
            if (!float.TryParse(args[2], out var minutes))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[0])));
                return;
            }
            duration = TimeSpan.FromMinutes(minutes);
        }

        if (_replay.TryStartRecording(_resMan.UserData, dir, overwrite, duration))
            shell.WriteLine(Loc.GetString("cmd-replay-recording-start-success"));
        else
            shell.WriteLine(Loc.GetString("cmd-replay-recording-start-error"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-replay-recording-start-hint-name"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-replay-recording-start-hint-overwrite"));

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString("cmd-replay-recording-start-hint-time"));

        return CompletionResult.Empty;
    }
}

internal sealed class ReplayStopCommand : LocalizedCommands
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;

    public override string Command => "replay_recording_stop";
    public override string Description => LocalizationManager.GetString($"cmd-replay-recording-stop-desc");
    public override string Help => LocalizationManager.GetString($"cmd-replay-recording-stop-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.IsRecording)
        {
            _replay.StopRecording();
            shell.WriteLine(Loc.GetString("cmd-replay-recording-stop-success"));
        }
        else
            shell.WriteLine(Loc.GetString("cmd-replay-recording-stop-not-recording"));
    }
}

internal sealed class ReplayStatsCommand : LocalizedCommands
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;

    public override string Command => "replay_recording_stats";
    public override string Description => LocalizationManager.GetString($"cmd-replay-recording-stats-desc");
    public override string Help => LocalizationManager.GetString($"cmd-replay-recording-stats-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_replay.IsRecording)
        {
            var stats = _replay.GetReplayStats();
            var sizeMb = stats.Size / (1024f * 1024f);
            var minutes = stats.Time.TotalMinutes;

            shell.WriteLine(Loc.GetString("cmd-replay-recording-stats-result",
                ("time", minutes.ToString("F1")),
                ("ticks", stats.Ticks),
                ("size", sizeMb.ToString("F1")),
                ("rate", (sizeMb / minutes).ToString("F2"))));
        }
        else
            shell.WriteLine(Loc.GetString("cmd-replay-recording-stop-not-recording"));
    }
}
