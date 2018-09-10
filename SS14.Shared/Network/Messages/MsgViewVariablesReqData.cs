using System.IO;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.ViewVariables;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent client to server to request data from the server.
    /// </summary>
    public class MsgViewVariablesReqData : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesReqData);

        public MsgViewVariablesReqData(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

        public uint ReqId { get; set; }
        public uint SessionId { get; set; }
        public ViewVariablesRequest RequestMeta { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ReqId = buffer.ReadUInt32();
            SessionId = buffer.ReadUInt32();
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            var length = buffer.ReadInt32();
            var bytes = buffer.ReadBytes(length);
            using (var stream = new MemoryStream(bytes))
            {
                RequestMeta = serializer.Deserialize<ViewVariablesRequest>(stream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ReqId);
            buffer.Write(SessionId);
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, RequestMeta);
                buffer.Write((int)stream.Length);
                buffer.Write(stream.ToArray());
            }
        }
    }
}

