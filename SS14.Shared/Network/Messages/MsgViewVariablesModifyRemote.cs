using System.IO;
using System.Security.Cryptography.X509Certificates;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;

namespace SS14.Shared.Network.Messages
{
    public class MsgViewVariablesModifyRemote : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesModifyRemote);
        public MsgViewVariablesModifyRemote(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint SessionId { get; set; }
        public string PropertyName { get; set; }
        public object Value { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            SessionId = buffer.ReadUInt32();
            PropertyName = buffer.ReadString();
            var length = buffer.ReadInt32();
            var bytes = buffer.ReadBytes(length);
            using (var stream = new MemoryStream(bytes))
            {
                Value = serializer.Deserialize(stream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            buffer.Write(SessionId);
            buffer.Write(PropertyName);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, Value);
                buffer.Write((int)stream.Length);
                buffer.Write(stream.ToArray());
            }
        }
    }
}
