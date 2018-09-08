using System;
using System.IO;
using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace SS14.Shared.Network.Messages
{
    /// <summary>
    ///     Sent from client to server to request to open a session.
    /// </summary>
    public class MsgViewVariablesReqSession : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgViewVariablesReqSession);
        public MsgViewVariablesReqSession(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public uint ReqId { get; set; }

        /// <summary>
        ///     A selector that can be used to describe a server object.
        ///     This isn't BYOND, we don't have consistent \ref references.
        /// </summary>
        public ViewVariablesObjectSelector Selector { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ReqId = buffer.ReadUInt32();
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            var length = buffer.ReadInt32();
            var bytes = buffer.ReadBytes(length);
            using (var stream = new MemoryStream(bytes))
            {
                Selector = serializer.Deserialize<ViewVariablesObjectSelector>(stream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ReqId);
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, Selector);
                buffer.Write((int)stream.Length);
                buffer.Write(stream.ToArray());
            }
        }
    }
}
