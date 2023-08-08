using System;
using JetBrains.Annotations;
using Robust.Client.Replays.Playback;
using Robust.Shared.Console;

namespace Robust.Client.Replays.Commands;

[UsedImplicitly]
public sealed class ReplaySetTimeCommand : BaseReplayCommand
{
    public override string Command => IReplayPlaybackManager.SetCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!AssertPlaying(shell, out var replay))
            return;

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (int.TryParse(args[0], out var index))
        {
            PlaybackManager.SetIndex(index);
            return;
        }

        if (!TimeSpan.TryParse(args[0], out var target))
        {
            shell.WriteError(Loc.GetString("cmd-replay-error-time", ("time", args[0])));
            return;
        }

        PlaybackManager.SetTime(target);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHint(Loc.GetString("cmd-replay-set-time-hint"));
    }
}
