using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent from client to server or server to client to notify to close a session.
    /// </summary>
    public class MsgViewVariablesCloseSession : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesCloseSession);
        public MsgViewVariablesCloseSession(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint SessionId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            SessionId = buffer.ReadUInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(SessionId);
        }
    }
}
