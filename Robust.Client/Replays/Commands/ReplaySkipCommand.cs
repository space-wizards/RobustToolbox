using System;
using JetBrains.Annotations;
using Robust.Client.Replays.Playback;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Replays.Commands;

[UsedImplicitly]
public sealed class ReplaySkipCommand : BaseReplayCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => IReplayPlaybackManager.SkipCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!AssertPlaying(shell, out var replay))
            return;

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!int.TryParse(args[0], out var ticks))
        {
            if (!TimeSpan.TryParse(args[0], out var time))
            {
                shell.WriteError(Loc.GetString("cmd-replay-error-time", ("time", args[0])));
                return;
            }

            ticks = PlaybackManager.GetIndex(replay.CurrentReplayTime + time) - replay.CurrentIndex;
        }

        if (ticks < 0 || ticks > _cfg.GetCVar(CVars.ReplaySkipThreshold))
            PlaybackManager.SetIndex(replay.CurrentIndex + ticks);
        else if (ticks == 0)
            PlaybackManager.Playing = false;
        else
        {
            PlaybackManager.Playing = true;
            PlaybackManager.AutoPauseCountdown ??= 0;
            PlaybackManager.AutoPauseCountdown += (uint)ticks;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHint(Loc.GetString("cmd-replay-skip-hint"));
    }
}

