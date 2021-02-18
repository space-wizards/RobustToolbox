using Lidgren.Network;

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

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
