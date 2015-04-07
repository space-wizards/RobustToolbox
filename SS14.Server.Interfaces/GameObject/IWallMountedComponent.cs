using SS14.Server.Interfaces.Map;
using SS14.Shared;

namespace SS14.Server.Interfaces.GOC
{
    public interface IWallMountedComponent
    {
        void AttachToTile(TileRef tilePos);
    }
}
