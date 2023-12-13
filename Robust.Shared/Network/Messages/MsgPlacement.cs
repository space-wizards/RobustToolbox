using System;
using System.Numerics;
using Lidgren.Network;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgPlacement : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public PlacementManagerMessage PlaceType { get; set; }
        public string Align { get; set; }

        /// <summary>
        /// Should we replace existing entities if possible
        /// </summary>
        public bool Replacement { get; set; }
        public bool IsTile { get; set; }
        public int TileType { get; set; }
        public string EntityTemplateName { get; set; }
        public NetCoordinates NetCoordinates { get; set; }
        public Direction DirRcv { get; set; }
        public NetEntity EntityUid { get; set; }

        public int Range { get; set; }
        public string ObjType { get; set; }
        public string AlignOption { get; set; }
        public Vector2 RectSize { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            PlaceType = (PlacementManagerMessage) buffer.ReadByte();
            switch (PlaceType)
            {
                case PlacementManagerMessage.RequestPlacement:
                    Align = buffer.ReadString();
                    IsTile = buffer.ReadBoolean();
                    Replacement = buffer.ReadBoolean();

                    if (IsTile) TileType = buffer.ReadInt32();
                    else EntityTemplateName = buffer.ReadString();

                    NetCoordinates = buffer.ReadNetCoordinates();
                    DirRcv = (Direction)buffer.ReadByte();
                    break;
                case PlacementManagerMessage.StartPlacement:
                    Range = buffer.ReadInt32();
                    IsTile = buffer.ReadBoolean();
                    ObjType = buffer.ReadString();
                    AlignOption = buffer.ReadString();
                    break;
                case PlacementManagerMessage.CancelPlacement:
                case PlacementManagerMessage.PlacementFailed:
                    throw new NotImplementedException();
                case PlacementManagerMessage.RequestEntRemove:
                    EntityUid = new NetEntity(buffer.ReadInt32());
                    break;
                case PlacementManagerMessage.RequestRectRemove:
                    NetCoordinates = buffer.ReadNetCoordinates();
                    RectSize = buffer.ReadVector2();
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write((byte)PlaceType);
            switch (PlaceType)
            {
                case PlacementManagerMessage.RequestPlacement:
                    buffer.Write(Align);
                    buffer.Write(IsTile);
                    buffer.Write(Replacement);

                    if(IsTile) buffer.Write(TileType);
                    else buffer.Write(EntityTemplateName);

                    buffer.Write(NetCoordinates);
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
                    throw new NotImplementedException();
                case PlacementManagerMessage.RequestEntRemove:
                    buffer.Write((int)EntityUid);
                    break;
                case PlacementManagerMessage.RequestRectRemove:
                    buffer.Write(NetCoordinates);
                    buffer.Write(RectSize);
                    break;
            }
        }
    }
}
