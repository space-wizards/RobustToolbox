using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent client to server to request data from the server.
    /// </summary>
    public class MsgViewVariablesReqData : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesReqData);

        public MsgViewVariablesReqData(INetChannel channel) : base(NAME, GROUP)
        {
        }

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
