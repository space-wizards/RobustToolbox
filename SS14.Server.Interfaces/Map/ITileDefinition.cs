
namespace SS14.Server.Interfaces.Map
{
    public interface ITileDefinition
    {
        ushort TileId { get; }
        string Name { get; }
        bool IsConnectingSprite { get; }
        bool IsOpaque { get; }
        bool IsCollidable { get; }
        bool IsGasVolume { get; }
        bool IsVentedIntoSpace { get; }
        //bool IsFloor { get; } // TODO: Determine if we want this.
        bool IsWall { get; }
    }
}