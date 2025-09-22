using System.IO;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent client to server to request data from the server.
    /// </summary>
    public sealed class MsgViewVariablesReqData : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     The request ID that will be sent in <see cref="MsgViewVariablesRemoteData"/> to
        ///     identify this request among multiple potentially concurrent ones.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     The session ID for the session to read the data from.
        /// </summary>
        public uint SessionId { get; set; }

        /// <summary>
        ///     A metadata object that can be used by the server to know what data is being requested.
        /// </summary>
        public ViewVariablesRequest RequestMeta { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            RequestId = buffer.ReadUInt32();
            SessionId = buffer.ReadUInt32();
            var length = buffer.ReadInt32();
            using var stream = RobustMemoryManager.GetMemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
            RequestMeta = serializer.Deserialize<ViewVariablesRequest>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(RequestId);
            buffer.Write(SessionId);

            var stream = new MemoryStream();
            serializer.Serialize(stream, RequestMeta);

            buffer.Write((int)stream.Length);
            buffer.Write(stream.AsSpan());
        }
    }
}
