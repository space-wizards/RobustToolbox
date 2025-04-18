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
    [Serializable, NetSerializable]
    public sealed class MsgPlacement : EntityEventArgs
    {
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
    }
}
