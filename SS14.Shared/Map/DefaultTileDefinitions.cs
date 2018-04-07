namespace SS14.Shared.Map
{
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
            SpriteName = "floor_texture";
        }
    }

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
        }
    }
}
