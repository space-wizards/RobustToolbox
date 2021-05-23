using System.Collections.Immutable;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages.Handshake
{
    [NetMessage(MsgGroups.Core, "")]
    internal sealed class MsgLoginStart : NetMessage
    {
        // **NOTE**: This is a special message sent during the client<->server handshake.
        // It doesn't actually get sent normally and as such doesn't have the "normal" boilerplate.
        // It's basically just a sane way to encapsulate the message write/read logic.
        public MsgLoginStart() : base("", MsgGroups.Core)
        {
        }

        public string UserName;
        public ImmutableArray<byte> HWId;
        public bool CanAuth;
        public bool NeedPubKey;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            UserName = buffer.ReadString();
            var length = buffer.ReadByte();
            HWId = ImmutableArray.Create(buffer.ReadBytes(length));
            CanAuth = buffer.ReadBoolean();
            NeedPubKey = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(UserName);
            buffer.Write((byte) HWId.Length);
            buffer.Write(HWId.AsSpan());
            buffer.Write(CanAuth);
            buffer.Write(NeedPubKey);
        }
    }
}
