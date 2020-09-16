using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    internal sealed class MsgLogin : NetMessage
    {
        // **NOTE**: This is a special message sent during the client<->server handshake.
        // It doesn't actually get sent normally and as such doesn't have the "normal" boilerplate.
        // It's basically just a sane way to encapsulate the message write/read logic.
        public MsgLogin() : base("", MsgGroups.Core)
        {
        }

        public string UsernameRequest { get; set; }
        public byte[] AuthToken { get; set; }
        public byte[] EncryptionKey { get; set; }


        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UsernameRequest = buffer.ReadString();
            var tokenLength = buffer.ReadVariableInt32();
            AuthToken = buffer.ReadBytes(tokenLength);
            var keyLength = buffer.ReadVariableInt32();
            EncryptionKey = buffer.ReadBytes(keyLength);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UsernameRequest);
            buffer.WriteVariableInt32(AuthToken.Length);
            buffer.Write(AuthToken);
            buffer.WriteVariableInt32(EncryptionKey.Length);
            buffer.Write(EncryptionKey);
        }
    }
}
