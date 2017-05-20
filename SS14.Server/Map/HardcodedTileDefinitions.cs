using SS14.Server.Interfaces.Map;

namespace SS14.Server.Services.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public sealed class SpaceTileDefinition : ITileDefinition
    {
        public static readonly ITileDefinition Instance = new SpaceTileDefinition();
        private SpaceTileDefinition() { }

        public ushort TileId { get { return 0; } }

        public string Name { get { return "Space"; } }

        public bool IsConnectingSprite { get { return false; } }
        public bool IsOpaque { get { return false; } }
        public bool IsCollidable { get { return false; } }
        public bool IsGasVolume { get { return true; } }
        public bool IsVentedIntoSpace { get { return true; } }
        public bool IsWall { get { return false; } }
    }

    public sealed class FloorTileDefinition : TileDefinition
    {
        public FloorTileDefinition()
        {
            Name = "Floor";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = false;
            IsWall = false;
        }
    }

    public sealed class WallTileDefinition : TileDefinition
    {
        public WallTileDefinition()
        {
            Name = "Wall";

            IsConnectingSprite = false;
            IsOpaque = true;
            IsCollidable = true;
            IsGasVolume = false;
            IsVentedIntoSpace = false;
            IsWall = true;
        }
    }
}
