using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgEncryptionResponse : NetMessage
    {
        public override string MsgName => string.Empty;

        public override MsgGroups MsgGroup => MsgGroups.Core;

        public Guid UserId;
        public byte[] SealedData;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserId = buffer.ReadGuid();
            var keyLength = buffer.ReadVariableInt32();
            SealedData = buffer.ReadBytes(keyLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserId);
            buffer.WriteVariableInt32(SealedData.Length);
            buffer.Write(SealedData);
        }
    }
}
