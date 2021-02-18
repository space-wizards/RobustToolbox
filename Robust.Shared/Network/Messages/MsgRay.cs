using Lidgren.Network;
using Robust.Shared.Maths;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgRay : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgRay);

        public MsgRay(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

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
