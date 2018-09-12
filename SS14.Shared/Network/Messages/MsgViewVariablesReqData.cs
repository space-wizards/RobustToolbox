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

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            RequestId = buffer.ReadUInt32();
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
            buffer.Write(RequestId);
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

