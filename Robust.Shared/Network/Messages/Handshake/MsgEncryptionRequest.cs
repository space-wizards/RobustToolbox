using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    internal sealed class MsgEncryptionRequest : NetMessage
    {
        public MsgEncryptionRequest() : base("", MsgGroups.Core)
        {
        }

        public byte[] VerifyToken;
        public byte[] PublicKey;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var tokenLength = buffer.ReadVariableInt32();
            VerifyToken = buffer.ReadBytes(tokenLength);
            var keyLength = buffer.ReadVariableInt32();
            PublicKey = buffer.ReadBytes(keyLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(VerifyToken.Length);
            buffer.Write(VerifyToken);
            buffer.WriteVariableInt32(PublicKey.Length);
            buffer.Write(PublicKey);
        }
    }
}
