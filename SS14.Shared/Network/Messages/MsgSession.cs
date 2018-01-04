using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgSession : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "PlayerSessionMessage";
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly NetMessages ID = NetMessages.PlayerSessionMessage;

        public MsgSession(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion

        public PlayerSessionMessage MsgType { get; set; }
        public int Uid { get; set; }
        public PostProcessingEffectType PpType { get; set; }
        public float PpDuration { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MsgType = (PlayerSessionMessage) buffer.ReadByte();

            switch (MsgType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    Uid = buffer.ReadInt32();
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    PpType = (PostProcessingEffectType) buffer.ReadInt32();
                    PpDuration = buffer.ReadFloat();
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)MsgType);
            switch (MsgType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    buffer.Write(Uid);
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    buffer.Write((int)PpType);
                    buffer.Write(PpDuration);
                    break;
            }
        }
    }
}
