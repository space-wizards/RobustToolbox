using Lidgren.Network;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using System.IO;

namespace Robust.Shared.Network.Messages
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
            MsgSize = buffer.LengthBytes;
            var length = buffer.ReadInt32();
            var stateData = buffer.ReadBytes(length);
            using (var stateStream = new MemoryStream(stateData))
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                State = serializer.Deserialize<GameState>(stateStream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            using (var stateStream = new MemoryStream())
            {
                serializer.Serialize(stateStream, State);
                buffer.Write((int)stateStream.Length);
                buffer.Write(stateStream.ToArray());
            }
            MsgSize = buffer.LengthBytes;
        }
    }
}
