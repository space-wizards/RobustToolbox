using ICSharpCode.SharpZipLib.GZip;
using Lidgren.Network;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using System.IO;

namespace SS14.Shared.Network.Messages
{
    public class MsgState : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Entity;
        public static readonly string NAME = nameof(MsgState);
        public MsgState(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public GameState State { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var length = buffer.ReadInt32();
            var stateData = buffer.ReadBytes(length);
            using (var stateStream = new MemoryStream(stateData))
            {
                var serializer = IoCManager.Resolve<ISS14Serializer>();
                State = serializer.Deserialize<GameState>(stateStream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            using (var stateStream = new MemoryStream())
            {
                serializer.Serialize(stateStream, State);
                buffer.Write((int)stateStream.Length);
                buffer.Write(stateStream.ToArray());
            }
        }
    }
}
