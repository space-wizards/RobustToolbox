using System;
using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgEncryptionResponse : NetMessage
    {
        public override string MsgName => string.Empty;

        public override MsgGroups MsgGroup => MsgGroups.Core;

        public Guid UserId;
        public byte[] SealedData;

        private const int MaxFieldLength = 1024;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            UserId = buffer.ReadGuid();

            var keyLength = buffer.ReadVariableInt32();
            if (keyLength is < 0 or > (int)NetMessageExt.MaxSizeOfEncryption)
                throw new InvalidOperationException($"Sealed data length {keyLength} out of range.");

            SealedData = buffer.ReadBytes(keyLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(UserId);
            buffer.WriteVariableInt32(SealedData.Length);
            buffer.Write(SealedData);
        }
    }
}
