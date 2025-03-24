using System.Collections.Immutable;
using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgLoginStart : NetMessage
    {
        // **NOTE**: This is a special message sent during the client<->server handshake.
        // It doesn't actually get sent normally and as such doesn't have the "normal" boilerplate.
        // It's basically just a sane way to encapsulate the message write/read logic.
        public override string MsgName => string.Empty;

        public override MsgGroups MsgGroup => MsgGroups.Core;

        public string UserName;
        public bool CanAuth;
        public bool NeedPubKey;
        public bool Encrypt;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            UserName = buffer.ReadString();
            CanAuth = buffer.ReadBoolean();
            NeedPubKey = buffer.ReadBoolean();
            Encrypt = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(UserName);
            buffer.Write(CanAuth);
            buffer.Write(NeedPubKey);
            buffer.Write(Encrypt);
        }
    }
}
