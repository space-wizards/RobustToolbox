using System;
using Lidgren.Network;
using Robust.Shared.Network;
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
        public byte[] LegacyHwid;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            UserId = buffer.ReadGuid();
            var keyLength = buffer.ReadVariableByteLength(nameof(SealedData));
            SealedData = buffer.ReadBytes(keyLength);
            var legacyHwidLength = buffer.ReadVariableByteLength(nameof(LegacyHwid));
            LegacyHwid = buffer.ReadBytes(legacyHwidLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(UserId);
            buffer.WriteVariableInt32(SealedData.Length);
            buffer.Write(SealedData);
            buffer.WriteVariableInt32(LegacyHwid.Length);
            buffer.Write(LegacyHwid);
        }
    }
}
