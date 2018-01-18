using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgClGreet : NetMessage
    {
        #region REQUIRED
        public const MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgClGreet);
        public MsgClGreet(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public string PlyName { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlyName = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new NotImplementedException();
        }
    }
}
