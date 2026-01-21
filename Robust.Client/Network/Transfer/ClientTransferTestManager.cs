using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;

namespace Robust.Client.Network.Transfer;

internal sealed class ClientTransferTestManager(ITransferManager manager, ILogManager logManager)
    : TransferTestManager(manager, logManager)
{
    protected override bool PermissionCheck(INetChannel channel)
    {
        return true;
    }
}
