using Lidgren.Network;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    public class MsgReloadPrototypes : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgReloadPrototypes);

        public MsgReloadPrototypes(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

        public ResourcePath Path = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Path = new ResourcePath(buffer.ReadString());
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Path.ToString());
        }
    }
}
