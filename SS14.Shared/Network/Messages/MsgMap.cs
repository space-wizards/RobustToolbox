using Lidgren.Network;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;

namespace SS14.Shared.Network.Messages
{
    public class MsgMap : NetMessage
    {
        #region REQUIRED
        public const MsgGroups Group = MsgGroups.Entity;
        public static readonly string NAME = nameof(MsgMap);
        public MsgMap(INetChannel channel) : base(NAME, Group) { }
        #endregion

        public MapMessage MessageType { get; set; }
        public MapId MapIndex { get; set; }
        public GridId GridIndex { get; set; }

        public Turf SingleTurf { get; set; }

        public TileDef[] TileDefs { get; set; }
        public ChunkDef[] ChunkDefs { get; set; }

        public ushort ChunkSize { get; set; }

        public int MapGridsToSend { get; set; }


        public class Turf
        {
            public int X { get; set; }
            public int Y { get; set; }
            public uint Tile { get; set; }
        }

        public class TileDef
        {
            public string Name { get; set; }
            public uint Tile { get; set; }
        }

        public class ChunkDef
        {
            public int X { get; set; }
            public int Y { get; set; }
            public uint[] Tiles { get; set; }
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MessageType = (MapMessage) buffer.ReadByte();
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    MapIndex = new MapId(buffer.ReadInt32());
                    GridIndex = new GridId(buffer.ReadInt32());
                    SingleTurf = new Turf()
                    {
                        X = buffer.ReadInt32(),
                        Y = buffer.ReadInt32(),
                        Tile = buffer.ReadUInt32()
                    };
                    break;
                case MapMessage.SendTileMap:
                    GridIndex = new GridId(buffer.ReadInt32());
                    MapIndex = new MapId(buffer.ReadInt32());

                    //tile defs
                    var numTileDefs = buffer.ReadInt32();
                    var tileDefs = new TileDef[numTileDefs];
                    for (var i = 0; i < numTileDefs; i++)
                    {
                        tileDefs[i] = new TileDef()
                        {
                            Name = buffer.ReadString()
                        };
                    }
                    TileDefs = tileDefs;

                    // map chunks
                    ChunkSize = buffer.ReadUInt16();
                    var numChunks = buffer.ReadInt32();
                    ChunkDefs = new ChunkDef[numChunks];

                    for (var i = 0; i < numChunks; i++)
                    {
                        var newChunk = new ChunkDef()
                        {
                            X = buffer.ReadInt32(),
                            Y = buffer.ReadInt32()
                        };

                        var chunkCount = ChunkSize * ChunkSize;
                        var tiles = new uint[chunkCount];
                        for (var j = 0; j < chunkCount; j++)
                        {
                            tiles[j] = buffer.ReadUInt32();
                        }
                        newChunk.Tiles = tiles;
                        ChunkDefs[i] = newChunk;
                    }
                    break;
                case MapMessage.SendMapInfo:
                    MapGridsToSend = buffer.ReadInt32();
                    break;
                case MapMessage.CreateMap:
                case MapMessage.DeleteMap:
                    MapIndex = new MapId(buffer.ReadInt32());
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)MessageType);
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    buffer.Write((int)MapIndex);
                    buffer.Write((int)GridIndex);
                    buffer.Write(SingleTurf.X);
                    buffer.Write(SingleTurf.Y);
                    buffer.Write(SingleTurf.Tile);
                    break;
                case MapMessage.SendTileMap:
                    buffer.Write((int)GridIndex);
                    buffer.Write((int)MapIndex);

                    // Tile defs, ordered list
                    buffer.Write(TileDefs.Length);
                    foreach (TileDef tileId in TileDefs)
                        buffer.Write(tileId.Name);

                    // Map chunks
                    buffer.Write(ChunkSize);
                    buffer.Write(ChunkDefs.Length);
                    foreach (var chunk in ChunkDefs)
                    {
                        buffer.Write(chunk.X);
                        buffer.Write(chunk.Y);

                        // ordered list
                        foreach (var tile in chunk.Tiles)
                            buffer.Write(tile);
                    }

                    break;
                case MapMessage.SendMapInfo:
                    buffer.Write(MapGridsToSend);
                    break;
                case MapMessage.CreateMap:
                case MapMessage.DeleteMap:
                    buffer.Write((int)MapIndex);
                    break;
            }
        }
    }
}
