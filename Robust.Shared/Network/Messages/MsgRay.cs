using Lidgren.Network;
using Robust.Shared.Maths;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgRay : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public Vector2 RayOrigin { get; set; }
        public Vector2 RayHit { get; set; }
        public bool DidHit { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            DidHit = buffer.ReadBoolean();
            RayOrigin = buffer.ReadVector2();
            RayHit = buffer.ReadVector2();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(DidHit);
            buffer.Write(RayOrigin);
            buffer.Write(RayHit);
        }
    }
}
