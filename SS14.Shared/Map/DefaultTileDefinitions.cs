namespace SS14.Shared.Map
{
    /// <summary>
    ///     Default TileDefinition of Space
    /// </summary>
    public sealed class SpaceTileDefinition : TileDefinition
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public SpaceTileDefinition()
        {
            Name = "Space";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = true;
            IsWall = false;
            SpriteName = "space_texture";
        }
    }

    /// <summary>
    ///     Default TileDefinition of a Floor
    /// </summary>
    public sealed class FloorTileDefinition : TileDefinition
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public FloorTileDefinition()
        {
            Name = "Floor";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = false;
            IsWall = false;
            SpriteName = "floor_texture";
        }
    }

    /// <summary>
    ///     Default TileDefinition of a Wall
    /// </summary>
    public sealed class WallTileDefinition : TileDefinition
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public WallTileDefinition()
        {
            Name = "Wall";

            IsConnectingSprite = false;
            IsOpaque = true;
            IsCollidable = true;
            IsGasVolume = false;
            IsVentedIntoSpace = false;
            IsWall = true;
            SpriteName = "wall_texture";
        }
    }
}
