using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client to notify that a session was accepted and its new ID.
    /// </summary>
    public class MsgViewVariablesOpenSession : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesOpenSession);
        public MsgViewVariablesOpenSession(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint ReqId { get; set; }
        public uint SessionId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ReqId = buffer.ReadUInt32();
            SessionId = buffer.ReadUInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ReqId);
            buffer.Write(SessionId);
        }
    }
}
