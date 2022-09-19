using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client to contain object data read by VV.
    /// </summary>
    public sealed class MsgViewVariablesRemoteData : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     The request ID equal to the ID sent in <see cref="RequestId"/>,
        ///     to identify multiple, potentially concurrent, requests.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     The data blob containing the requested data.
        /// </summary>
        public ViewVariablesBlob Blob { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            RequestId = buffer.ReadUInt32();
            var length = buffer.ReadInt32();
            using var stream = buffer.ReadAlignedMemory(length);
            Blob = serializer.Deserialize<ViewVariablesBlob>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(RequestId);

            var stream = new MemoryStream();
            serializer.Serialize(stream, Blob);
            buffer.Write((int)stream.Length);
            buffer.Write(stream.AsSpan());
        }
    }
}
