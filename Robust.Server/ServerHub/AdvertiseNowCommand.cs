using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Server.ServerHub;

[InjectDependencies]
internal sealed partial class AdvertiseNowCommand : LocalizedCommands
{
    [Dependency] private HubManager _hubManager = default!;

    public override string Command => "hub_advertise_now";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _hubManager.AdvertiseNow();
    }
}
