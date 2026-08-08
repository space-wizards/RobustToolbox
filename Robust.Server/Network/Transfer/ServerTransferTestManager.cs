using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;

namespace Robust.Server.Network.Transfer;

internal sealed class ServerTransferTestManager(
    ITransferManager manager,
    ILogManager logManager,
    IConGroupController controller,
    IPlayerManager playerManager)
    : TransferTestManager(manager, logManager)
{
    protected override bool PermissionCheck(INetChannel channel)
    {
        if (!playerManager.TryGetSessionByChannel(channel, out var session))
            return false;

        return controller.CanCommand(session, TransferTestCommand.CommandKey);
    }
}
