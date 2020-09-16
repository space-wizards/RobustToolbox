using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    internal sealed class MsgLoginSuccess : NetMessage
    {
        // Same deal as MsgLogin, helper for NetManager only.

        public MsgLoginSuccess() : base("", MsgGroups.Core)
        {
        }

        public string UserName;
        public Guid UserId;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserName = buffer.ReadString();
            UserId = buffer.ReadGuid();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserName);
            buffer.Write(UserId);
        }
    }
}
