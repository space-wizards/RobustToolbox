using System;
using System.Net;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Robust.Server.Network
{
    public class ServerNetManager : NetManager, IServerNetManager
    {
        public event Func<IPEndPoint, string> JudgeConnection;

        protected override void HandleApproval(NetIncomingMessage message)
        {
            var banReason = JudgeConnection?.Invoke(message.SenderConnection.RemoteEndPoint);
            if (!(banReason is null))
            {
                message.SenderConnection.Deny($"You have been banned. Reason: {banReason}");
                return;
            }

            base.HandleApproval(message);
        }
    }
}
