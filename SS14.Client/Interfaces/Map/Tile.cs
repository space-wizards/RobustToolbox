using SS14.Shared.IoC;

namespace SS14.Client.Interfaces.Map
{
    [System.Diagnostics.DebuggerDisplay("Tile: {TileDef.Name}, Data={Data}")]
    public struct Tile
    {
        public readonly ushort TileId;
        public readonly ushort Data;

        private static ITileDefinitionManager TileDefManager;

        public Tile(ushort tileId = 0, ushort data = 0)
        {
            TileId = tileId;
            Data = data;
        }

        public ITileDefinition TileDef
        {
            get
            {
                if (TileDefManager == null)
                    TileDefManager = IoCManager.Resolve<ITileDefinitionManager>();

                return TileDefManager[TileId];
            }
        }

        public bool IsSpace { get { return TileId == 0; } }

        public static explicit operator uint(Tile tile)
        {
            return (uint)tile.TileId << 16 | tile.Data;
        }
        public static explicit operator Tile(uint tile)
        {
            return new Tile(
                (ushort)(tile >> 16),
                (ushort)(tile)
                );
        }

        public override bool Equals(object obj)
        {
            return obj is Tile && this == (Tile)obj;
        }
        public override int GetHashCode()
        {
            return ((uint)this).GetHashCode();
        }
        public static bool operator ==(Tile a, Tile b)
        {
            return a.TileId == b.TileId && a.Data == b.Data;
        }
        public static bool operator !=(Tile a, Tile b)
        {
            return !(a == b);
        }

    }
}
