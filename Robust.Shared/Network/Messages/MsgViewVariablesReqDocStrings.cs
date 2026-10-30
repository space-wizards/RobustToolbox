using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    /// <summary>
    ///     Sent client to server to ask for DocStrings the client doesn't have locally,
    ///     usually because they belong to server-only code.
    ///     The server answers with <see cref="MsgViewVariablesDocStrings"/>.
    /// </summary>
    public sealed class MsgViewVariablesReqDocStrings : NetMessage
    {
        /// <summary>
        ///     The most keys a single request may contain. The server ignores anything past this.
        /// </summary>
        public const int MaxKeys = 1024;

        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     Identifies this request, echoed back in <see cref="MsgViewVariablesDocStrings.RequestId"/>.
        /// </summary>
        public uint RequestId { get; set; }

        public ViewVariablesDocStringKind Kind { get; set; }

        /// <summary>
        ///     The keys to look up, in the same format as the xml doc member names without the <c>T:</c>/<c>F:</c>/<c>P:</c> prefix.
        /// </summary>
        public List<string> Keys { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            RequestId = buffer.ReadUInt32();
            Kind = (ViewVariablesDocStringKind) buffer.ReadByte();
            if (Kind is not (ViewVariablesDocStringKind.Type or ViewVariablesDocStringKind.FieldOrProperty))
                throw new InvalidOperationException($"DocString request has invalid kind {Kind}");

            var count = buffer.ReadVariableInt32();
            if (count < 0 || count > MaxKeys)
                throw new InvalidOperationException($"DocString request has invalid key count {count}");

            Keys = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                Keys.Add(buffer.ReadString());
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            DebugTools.Assert(Kind != ViewVariablesDocStringKind.Invalid, $"{nameof(Kind)} was not set");

            buffer.Write(RequestId);
            buffer.Write((byte) Kind);
            buffer.WriteVariableInt32(Keys.Count);
            foreach (var key in Keys)
            {
                buffer.Write(key);
            }
        }
    }
}
