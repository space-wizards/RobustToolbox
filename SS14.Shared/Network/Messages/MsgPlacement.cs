using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlacement : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.PlacementManagerMessage;
        public static readonly MsgGroups GROUP = MsgGroups.Core;

        public static readonly string NAME = ID.ToString();
        public MsgPlacement(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public PlacementManagerMessage PlaceType { get; set; }
        public string Align { get; set; }
        public bool IsTile { get; set; }
        public ushort TileType { get; set; }
        public string EntityTemplateName { get; set; }
        public float XRcv { get; set; }
        public float YRcv { get; set; }
        public Direction DirRcv { get; set; }

        public int Range { get; set; }
        public string ObjType { get; set; }
        public string AlignOption { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlaceType = (PlacementManagerMessage) buffer.ReadByte();
            if(PlaceType == PlacementManagerMessage.RequestPlacement)
            {
                Align = buffer.ReadString();
                IsTile = buffer.ReadBoolean();

                if (IsTile) TileType = buffer.ReadUInt16();
                else EntityTemplateName = buffer.ReadString();

                XRcv = buffer.ReadFloat();
                YRcv = buffer.ReadFloat();
                DirRcv = (Direction)buffer.ReadByte();
            }
            else if (PlaceType == PlacementManagerMessage.StartPlacement)
            {
                Range = buffer.ReadInt32();
                IsTile = buffer.ReadBoolean();
                ObjType = buffer.ReadString();
                AlignOption = buffer.ReadString();
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
                    buffer.Write(Align);
                    buffer.Write(IsTile);
                    if(IsTile) buffer.Write(TileType);
                    else buffer.Write(EntityTemplateName);
                    buffer.Write(XRcv);
                    buffer.Write(YRcv);
                    buffer.Write((byte)DirRcv);
                    break;
                case PlacementManagerMessage.StartPlacement:
                    buffer.Write(Range);
                    buffer.Write(IsTile);
                    buffer.Write(ObjType);
                    buffer.Write(AlignOption);
                    break;
                case PlacementManagerMessage.CancelPlacement:
                case PlacementManagerMessage.PlacementFailed:
                    break;
            }
        }
    }
}
