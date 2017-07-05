using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgSession : NetMessage
    {
        #region REQUIRED
        public static readonly string NAME = "PlayerSessionMessage";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.PlayerSessionMessage;

        public MsgSession(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion

        public PlayerSessionMessage msgType;
        public string verb;
        public int uid;
        public PostProcessingEffectType PpType;
        public float PpDuration;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            msgType = (PlayerSessionMessage) buffer.ReadByte();

            switch (msgType)
            {
                case PlayerSessionMessage.Verb:
                    verb = buffer.ReadString();
                    uid = buffer.ReadInt32();
                    break;
                case PlayerSessionMessage.AttachToEntity:
                    uid = buffer.ReadInt32();
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    PpType = (PostProcessingEffectType) buffer.ReadInt32();
                    PpDuration = buffer.ReadFloat();
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)msgType);
            switch (msgType)
            {
                case PlayerSessionMessage.Verb:
                    buffer.Write(verb);
                    buffer.Write(uid);
                    break;
                case PlayerSessionMessage.AttachToEntity:
                    buffer.Write(uid);
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    buffer.Write((int)PpType);
                    buffer.Write(PpDuration);
                    break;
            }
        }
    }
}
