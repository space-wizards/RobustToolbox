using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Server.ServerHub;

internal sealed class AdvertiseNowCommand : LocalizedCommands
{
    [Dependency] private readonly HubManager _hubManager = default!;

    public override string Command => "hub_advertise_now";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _hubManager.AdvertiseNow();
    }
}
