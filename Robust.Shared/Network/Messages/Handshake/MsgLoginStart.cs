using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    internal sealed class MsgLoginStart : NetMessage
    {
        // **NOTE**: This is a special message sent during the client<->server handshake.
        // It doesn't actually get sent normally and as such doesn't have the "normal" boilerplate.
        // It's basically just a sane way to encapsulate the message write/read logic.
        public MsgLoginStart() : base("", MsgGroups.Core)
        {
        }

        public string UserName;
        public bool CanAuth;
        public bool NeedPubKey;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserName = buffer.ReadString();
            CanAuth = buffer.ReadBoolean();
            NeedPubKey = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserName);
            buffer.Write(CanAuth);
            buffer.Write(NeedPubKey);
        }
    }
}
