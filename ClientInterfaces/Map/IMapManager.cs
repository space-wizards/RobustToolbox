using System.Drawing;
using ClientInterfaces.Lighting;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;

namespace ClientInterfaces.Map
{
    public interface IMapManager
    {
        int GetTileSpacing();
        void Shutdown();
        bool IsSolidTile(Vector2D pos);
        void HandleNetworkMessage(NetIncomingMessage message);
        void HandleAtmosDisplayUpdate(NetIncomingMessage message);
        ITile GetTileAt(Vector2D pos);
        ITile GetTileAt(int x, int y);
        bool LoadNetworkedMap(TileType[,] networkedArray, TileState[,] networkedStates, int _mapWidth, int _mapHeight);
        Vector2D GetTileArrayPositionFromWorldPosition(float x, float z);
        Point GetTileArrayPositionFromWorldPosition(Vector2D pos);
        int GetMapWidth();
        int GetMapHeight();
        Point GetLastVisiblePoint();
        bool NeedVisibilityUpdate();
        void SetLastVisiblePoint(Point point);
        void ComputeVisibility(int viewerX, int viewerY);
        void SetAllVisible();
        void Init();

        TileType GetTileTypeFromWorldPosition(Vector2D vector2D);
    }
}
