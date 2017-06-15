using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    /// The definition (template) for a grid tile.
    /// </summary>
    public interface ITileDefinition
    {
        ushort TileId { get; }
        void InvalidateTileId();

        string Name { get; }
        bool IsConnectingSprite { get; }
        bool IsOpaque { get; }
        bool IsCollidable { get; }
        bool IsGasVolume { get; }
        bool IsVentedIntoSpace { get; }
        //bool IsFloor { get; } // TODO: Determine if we want this.
        bool IsWall { get; }
        string SpriteName { get; }

        Tile Create(ushort data = 0);
    }
}
