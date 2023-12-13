using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.Replays.Playback;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Replays;

namespace Robust.Client.Replays.Commands;

public abstract class BaseReplayCommand : LocalizedCommands
{
    [Dependency] protected readonly IReplayPlaybackManager PlaybackManager = default!;
    protected ILocalizationManager Loc => LocalizationManager;

    public override string Description => Loc.GetString($"cmd-{Command.Replace('_','-')}-desc");

    public override string Help => Loc.GetString($"cmd-{Command.Replace('_','-')}-help");

    protected bool AssertPlaying(IConsoleShell shell, [NotNullWhen(true)] out ReplayData? replay)
    {
        if (PlaybackManager.Replay != null)
        {
            replay = PlaybackManager.Replay;
            return true;
        }

        replay = null;
        shell.WriteError(Loc.GetString("cmd-replay-error-no-replay"));
        return false;
    }

    protected bool AssertPlaying(IConsoleShell shell)
        => AssertPlaying(shell, out _);
}

[UsedImplicitly]
public sealed class ReplayPlayCommand : BaseReplayCommand
{
    public override string Command => IReplayPlaybackManager.PlayCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (AssertPlaying(shell))
            PlaybackManager.Playing = true;
    }
}

[UsedImplicitly]
public sealed class ReplayPauseCommand : BaseReplayCommand
{
    public override string Command => IReplayPlaybackManager.PauseCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (AssertPlaying(shell))
            PlaybackManager.Playing = false;
    }
}

[UsedImplicitly]
public sealed class ReplayToggleCommand : BaseReplayCommand
{
    public override string Command => IReplayPlaybackManager.ToggleCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (AssertPlaying(shell))
            PlaybackManager.Playing = !PlaybackManager.Playing;
    }
}

[UsedImplicitly]
public sealed class ReplayStopCommand : BaseReplayCommand
{
    public override string Command => IReplayPlaybackManager.StopCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (AssertPlaying(shell))
            PlaybackManager.StopReplay();
    }
}
