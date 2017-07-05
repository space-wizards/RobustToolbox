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
        public const MsgGroups Group = MsgGroups.ENTITY;

        public static readonly string NAME = ID.ToString();
        public MsgMap(INetChannel channel) : base(NAME, Group, ID) { }
        #endregion

        public MapMessage MessageType;
        public int Version;

        public Turf SingleTurf;

        public TileDef[] TileDefs;
        public ChunkDef[] ChunkDefs;
 
        public class Turf
        {
            public int x;
            public int y;
            public uint tile;
        }

        public class TileDef
        {
            public string name;
            public uint tile;
        }

        public class ChunkDef
        {
            public int X;
            public int Y;
            public TileDef[] TileDefs;
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MessageType = (MapMessage) buffer.ReadByte();
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    SingleTurf = new Turf()
                    {
                        x = buffer.ReadInt32(),
                        y = buffer.ReadInt32(),
                        tile = buffer.ReadUInt32()
                    };
                    break;
                case MapMessage.SendTileMap:
                    //not dealing with this right now...
                    throw new NotImplementedException();
                    break;
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)MessageType);
            switch (MessageType)
            {
                case MapMessage.TurfUpdate:
                    buffer.Write(SingleTurf.x);
                    buffer.Write(SingleTurf.y);
                    buffer.Write(SingleTurf.tile);
                    break;
                case MapMessage.SendTileMap:
                    buffer.Write(Version);
                    buffer.Write(TileDefs.Length);

                    // Tile defs, ordered list
                    foreach (TileDef tileId in TileDefs)
                        buffer.Write(tileId.name);

                    // Map chunks
                    buffer.Write(ChunkDefs.Length);
                    foreach (var chunk in ChunkDefs)
                    {
                        buffer.Write(chunk.X);
                        buffer.Write(chunk.Y);

                        // ordered list
                        foreach (var tile in chunk.TileDefs)
                            buffer.Write(tile.tile);
                    }

                    break;
            }
        }
    }
}
