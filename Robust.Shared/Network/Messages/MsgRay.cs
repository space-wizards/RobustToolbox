using System;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;

namespace Robust.Shared.Network.Messages
{
    public class MsgRay : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgRay);
        public uint RequestId { get; set; }
        public uint SessionId { get; set; }

        public Ray RayToSend { get; set; }

        public MsgRay(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var origin = buffer.ReadVector2();
            var dir = buffer.ReadVector2();
            var mask = buffer.ReadInt32();
            RayToSend = new Ray(origin, dir, mask);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(RayToSend.Position);
            buffer.Write(RayToSend.Direction);
            buffer.Write(RayToSend.CollisionMask);
        }
    }
}
