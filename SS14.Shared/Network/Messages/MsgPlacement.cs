using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlacement : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.PlacementManagerMessage;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public MsgPlacement(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public PlacementManagerMessage PlaceType;
        public string align;
        public bool isTile;
        public ushort tileType;
        public string entityTemplateName;
        public float xRcv;
        public float yRcv;
        public Direction dirRcv;

        public bool IsTile;
        public int range;
        public string objType;
        public string alignOption;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlaceType = (PlacementManagerMessage) buffer.ReadByte();
            if(PlaceType == PlacementManagerMessage.RequestPlacement)
            {
                align = buffer.ReadString();
                isTile = buffer.ReadBoolean();

                if (isTile) tileType = buffer.ReadUInt16();
                else entityTemplateName = buffer.ReadString();

                xRcv = buffer.ReadFloat();
                yRcv = buffer.ReadFloat();
                dirRcv = (Direction)buffer.ReadByte();
            }
            else if (PlaceType == PlacementManagerMessage.StartPlacement)
            {
                range = buffer.ReadInt32();
                IsTile = buffer.ReadBoolean();
                objType = buffer.ReadString();
                alignOption = buffer.ReadString();
            } 
            else if (PlaceType == PlacementManagerMessage.CancelPlacement)
                throw new NotImplementedException();
            else if(PlaceType == PlacementManagerMessage.PlacementFailed)
                throw new NotImplementedException();

        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)PlaceType);
            switch (PlaceType)
            {
                case PlacementManagerMessage.RequestPlacement:
                    buffer.Write(align);
                    buffer.Write(isTile);
                    if(isTile) buffer.Write(tileType);
                    else buffer.Write(entityTemplateName);
                    buffer.Write(xRcv);
                    buffer.Write(yRcv);
                    buffer.Write((byte)dirRcv);
                    break;
                case PlacementManagerMessage.StartPlacement:
                    buffer.Write(range);
                    buffer.Write(IsTile);
                    buffer.Write(objType);
                    buffer.Write(alignOption);
                    break;
                case PlacementManagerMessage.CancelPlacement:
                case PlacementManagerMessage.PlacementFailed:
                    break;
            }
        }
    }
}
