using SS14.Shared.Map;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IWallMountedComponent
    {
        void AttachToTile(TileRef tilePos);
    }
}
