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
    ///     Sent from client to server to request to open a session.
    /// </summary>
    public sealed class MsgViewVariablesReqSession : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     An ID the client assigns so it knows which request was accepted/denied through
        ///     <see cref="MsgViewVariablesOpenSession"/> and <see cref="MsgViewVariablesCloseSession"/>.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     A selector that can be used to describe a server object.
        ///     This isn't BYOND, we don't have consistent \ref references.
        /// </summary>
        public ViewVariablesObjectSelector Selector { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            RequestId = buffer.ReadUInt32();
            var length = buffer.ReadInt32();
            using var stream = RobustMemoryManager.GetMemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
            Selector = serializer.Deserialize<ViewVariablesObjectSelector>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(RequestId);

            var stream = new MemoryStream();
            serializer.Serialize(stream, Selector);
            buffer.Write((int)stream.Length);
            buffer.Write(stream.AsSpan());
        }
    }
}
