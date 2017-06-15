using SS14.Server.Interfaces.Map;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IWallMountedComponent
    {
        void AttachToTile(TileRef tilePos);
    }
}
