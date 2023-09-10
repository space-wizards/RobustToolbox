using Robust.Client.Replays.UI;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Replays.Commands;

public sealed class ReplayToggleUiCommand : BaseReplayCommand
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "replay_toggleui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var screen = _userInterfaceManager.ActiveScreen;
        if (screen == null || !screen.TryGetWidget(out ReplayControlWidget? replayWidget))
            return;

        replayWidget.Visible = !replayWidget.Visible;
    }
}
