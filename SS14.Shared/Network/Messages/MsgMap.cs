using System;
using Lidgren.Network;
using SFML.System;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgMap : NetMessage
    {
        #region REQUIRED
        public const NetMessages ID = NetMessages.MapMessage;
        public const MsgGroups Group = MsgGroups.Entity;

        public static readonly string NAME = ID.ToString();
        public MsgMap(INetChannel channel) : base(NAME, Group, ID) { }
        #endregion

        public MapMessage MessageType { get; set; }
        public int Version { get; set; }

        public Turf SingleTurf { get; set; }

        public TileDef[] TileDefs { get; set; }
        public ChunkDef[] ChunkDefs { get; set; }

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
            public TileDef[] Defs { get; set; }
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MessageType = (MapMessage) buffer.ReadByte();
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    SingleTurf = new Turf()
                    {
                        X = buffer.ReadInt32(),
                        Y = buffer.ReadInt32(),
                        Tile = buffer.ReadUInt32()
                    };
                    break;
                case MapMessage.SendTileMap:
                    //not dealing with this right now...
                    throw new NotImplementedException();
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)MessageType);
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    buffer.Write(SingleTurf.X);
                    buffer.Write(SingleTurf.Y);
                    buffer.Write(SingleTurf.Tile);
                    break;
                case MapMessage.SendTileMap:
                    buffer.Write(Version);
                    buffer.Write(TileDefs.Length);

                    // Tile defs, ordered list
                    foreach (TileDef tileId in TileDefs)
                        buffer.Write(tileId.Name);

                    // Map chunks
                    buffer.Write(ChunkDefs.Length);
                    foreach (var chunk in ChunkDefs)
                    {
                        buffer.Write(chunk.X);
                        buffer.Write(chunk.Y);

                        // ordered list
                        foreach (var tile in chunk.Defs)
                            buffer.Write(tile.Tile);
                    }

                    break;
            }
        }
    }
}
