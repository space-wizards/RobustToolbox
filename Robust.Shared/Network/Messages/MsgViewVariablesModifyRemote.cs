using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgViewVariablesModifyRemote : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesModifyRemote);
        public MsgViewVariablesModifyRemote(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        /// <summary>
        ///     The session ID of the session to modify.
        /// </summary>
        public uint SessionId { get; set; }

        /// <summary>
        ///     Whether the value is meant to be "reinterpreted" on the server.
        /// </summary>
        /// <remarks>
        ///     Modifying a remote prototype needs that we send an object of type <see cref="ViewVariablesBlobMembers.PrototypeReferenceToken"/>.
        ///     Setting this flag to true will make the server index and use the actual prototype the <see cref="Value"/> refers to.
        /// </remarks>
        public bool ReinterpretValue { get; set; }

        /// <summary>
        ///     Same deal as <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/>.
        /// </summary>
        public object[] PropertyIndex { get; set; }

        /// <summary>
        ///     The new value of the property.
        /// </summary>
        public object Value { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            SessionId = buffer.ReadUInt32();
            {
                var length = buffer.ReadInt32();
                using var stream = buffer.ReadAlignedMemory(length);
                PropertyIndex = serializer.Deserialize<object[]>(stream);
            }
            {
                var length = buffer.ReadInt32();
                using var stream = buffer.ReadAlignedMemory(length);
                Value = serializer.Deserialize(stream);
            }
            ReinterpretValue = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            buffer.Write(SessionId);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, PropertyIndex);
                buffer.Write((int)stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, Value);
                buffer.Write((int)stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
            buffer.Write(ReinterpretValue);
        }
    }
}
