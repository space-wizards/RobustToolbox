using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    internal sealed class MsgLoginResp : NetMessage
    {
        // Same deal as MsgLogin, helper for NetManager only.

        public MsgLoginResp() : base("", MsgGroups.Core)
        {
        }

        public string UserName { get; set; }
        public Guid UserId { get; set; }
        public bool Encrypt { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserName = buffer.ReadString();
            UserId = buffer.ReadGuid();
            Encrypt = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserName);
            buffer.Write(UserId);
            buffer.Write(Encrypt);
        }
    }
}
