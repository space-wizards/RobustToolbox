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
}
