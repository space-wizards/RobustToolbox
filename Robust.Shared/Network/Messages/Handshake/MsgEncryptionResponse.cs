using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    [NetMessage(MsgGroups.Core, "")]
    internal sealed class MsgEncryptionResponse : NetMessage
    {
        public MsgEncryptionResponse() : base("", MsgGroups.Core)
        {
        }

        public Guid UserId;
        public byte[] SharedSecret;
        public byte[] VerifyToken;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserId = buffer.ReadGuid();
            var keyLength = buffer.ReadVariableInt32();
            SharedSecret = buffer.ReadBytes(keyLength);
            var tokenLength = buffer.ReadVariableInt32();
            VerifyToken = buffer.ReadBytes(tokenLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserId);
            buffer.WriteVariableInt32(SharedSecret.Length);
            buffer.Write(SharedSecret);
            buffer.WriteVariableInt32(VerifyToken.Length);
            buffer.Write(VerifyToken);
        }
    }
}
