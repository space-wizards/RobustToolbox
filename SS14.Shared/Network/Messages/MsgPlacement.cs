using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlacement : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.PlacementManagerMessage;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgPlacement(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public PlacementManagerMessage PlaceType;
        public string align;
        public bool isTile;
        public ushort tileType;
        public string entityTemplateName;
        public float xRcv;
        public float yRcv;
        public Direction dirRcv;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlaceType = (PlacementManagerMessage) buffer.ReadByte();
            align = buffer.ReadString();
            isTile = buffer.ReadBoolean();

            if (isTile) tileType = buffer.ReadUInt16();
            else entityTemplateName = buffer.ReadString();

            xRcv = buffer.ReadFloat();
            yRcv = buffer.ReadFloat();
            dirRcv = (Direction)buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)PlaceType);
            buffer.Write(align);
            buffer.Write(isTile);
            if(isTile) buffer.Write(tileType);
            else buffer.Write(entityTemplateName);
        }
    }
}
