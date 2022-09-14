using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgEncryptionRequest : NetMessage
    {
        public override string MsgName => string.Empty;

        public override MsgGroups MsgGroup => MsgGroups.Core;

        public byte[] VerifyToken;
        public byte[] PublicKey;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var tokenLength = buffer.ReadVariableInt32();
            VerifyToken = buffer.ReadBytes(tokenLength);
            var keyLength = buffer.ReadVariableInt32();
            PublicKey = buffer.ReadBytes(keyLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.WriteVariableInt32(VerifyToken.Length);
            buffer.Write(VerifyToken);
            buffer.WriteVariableInt32(PublicKey.Length);
            buffer.Write(PublicKey);
        }
    }
}
