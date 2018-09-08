using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client to deny a <see cref="MsgViewVariablesReqSession"/>.
    /// </summary>
    public class MsgViewVariablesDenySession : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesDenySession);
        public MsgViewVariablesDenySession(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint ReqId { get; set; }
        public DenyReason Reason { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ReqId = buffer.ReadUInt32();
            Reason = (DenyReason)buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ReqId);
            buffer.Write((byte)Reason);
        }

        public enum DenyReason : byte
        {
            /// <summary>
            ///     Come back with admin access.
            /// </summary>
            NoAccess,

            /// <summary>
            ///     Object tried to read does not exist.
            /// </summary>
            NoObject,

            /// <summary>
            ///     Request was invalid or something.
            /// </summary>
            InvalidRequest,
        }
    }
}
