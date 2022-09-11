using Lidgren.Network;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client to deny a <see cref="MsgViewVariablesReqSession"/>.
    /// </summary>
    public sealed class MsgViewVariablesDenySession : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     The request ID to identify WHICH request has been denied.
        ///     Equal to <see cref="MsgViewVariablesReqSession.RequestId"/> on the message that requested this denied session.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     Reason for why the request was denied.
        /// </summary>
        public ViewVariablesResponseCode Reason { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            RequestId = buffer.ReadUInt32();
            Reason = (ViewVariablesResponseCode)buffer.ReadUInt16();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(RequestId);
            buffer.Write((ushort)Reason);
        }
    }
}
