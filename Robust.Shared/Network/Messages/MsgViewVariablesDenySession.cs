using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client to deny a <see cref="MsgViewVariablesReqSession"/>.
    /// </summary>
    [NetMessage(MsgGroups.Command)]
    public class MsgViewVariablesDenySession : NetMessage
    {
        /// <summary>
        ///     The request ID to identify WHICH request has been denied.
        ///     Equal to <see cref="MsgViewVariablesReqSession.RequestId"/> on the message that requested this denied session.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     Reason for why the request was denied.
        /// </summary>
        public DenyReason Reason { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            RequestId = buffer.ReadUInt32();
            Reason = (DenyReason)buffer.ReadUInt16();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(RequestId);
            buffer.Write((ushort)Reason);
        }

        public enum DenyReason : ushort
        {
            /// <summary>
            ///     Come back with admin access.
            /// </summary>
            NoAccess = 401,

            /// <summary>
            ///     Object pointing to by the selector does not exist.
            /// </summary>
            NoObject = 404,

            /// <summary>
            ///     Request was invalid or something.
            /// </summary>
            InvalidRequest = 400,
        }
    }
}
