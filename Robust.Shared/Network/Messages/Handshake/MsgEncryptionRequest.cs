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
        public bool WantHwid;
        public bool WantDiscord; // Starlight-edit

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var tokenLength = buffer.ReadVariableInt32();
            VerifyToken = buffer.ReadBytes(tokenLength);
            var keyLength = buffer.ReadVariableInt32();
            PublicKey = buffer.ReadBytes(keyLength);
            WantHwid = buffer.ReadBoolean();
            WantDiscord = buffer.Position < buffer.LengthBits && buffer.ReadBoolean(); // Starlight-edit
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.WriteVariableInt32(VerifyToken.Length);
            buffer.Write(VerifyToken);
            buffer.WriteVariableInt32(PublicKey.Length);
            buffer.Write(PublicKey);
            buffer.Write(WantHwid);
            buffer.Write(WantDiscord); // Starlight-edit
        }
    }
}
