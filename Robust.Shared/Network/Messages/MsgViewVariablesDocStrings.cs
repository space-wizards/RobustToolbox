using System;
using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent server to client in response to a <see cref="MsgViewVariablesReqDocStrings"/>.
    /// </summary>
    public sealed class MsgViewVariablesDocStrings : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     Equal to <see cref="MsgViewVariablesReqDocStrings.RequestId"/> on the request this answers.
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        ///     One entry per requested key, in request order. <c>null</c> means the server has no DocString for that key,
        ///     or the client isn't allowed to ask.
        /// </summary>
        public string[] DocStrings { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            RequestId = buffer.ReadUInt32();
            var count = buffer.ReadVariableInt32();
            if (count < 0 || count > MsgViewVariablesReqDocStrings.MaxKeys)
                throw new InvalidOperationException($"DocString response has invalid count {count}");

            DocStrings = new string[count];
            for (var i = 0; i < count; i++)
            {
                DocStrings[i] = buffer.ReadBoolean() ? buffer.ReadString() : null;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(RequestId);
            buffer.WriteVariableInt32(DocStrings.Length);
            foreach (var docString in DocStrings)
            {
                buffer.Write(docString != null);
                if (docString != null)
                    buffer.Write(docString);
            }
        }
    }
}
