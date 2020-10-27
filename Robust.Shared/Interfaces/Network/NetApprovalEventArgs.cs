using System;
using Lidgren.Network;

namespace Robust.Shared.Interfaces.Network
{
    public sealed class NetApprovalEventArgs : EventArgs
    {
        public NetConnection Connection { get; }

        public NetApprovalEventArgs(NetConnection connection)
        {
            Connection = connection;
        }
    }
}
