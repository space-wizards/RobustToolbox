using Lidgren.Network;
using SS14.Server.Interfaces.Tiles;
using SS14.Shared;
using System.Drawing;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.Map
{
    public interface IMapManager
    {
        bool InitMap(string mapName);
        void HandleNetworkMessage(NetIncomingMessage message);
        //void NetworkUpdateTile(ITile t);
        void SaveMap();

        void MoveGasCell(ITile fromTile, ITile toTile);

        NetOutgoingMessage CreateMapMessage(MapMessage messageType);
        void SendMessage(NetOutgoingMessage message);
        void Shutdown();
        int GetMapWidth();
        int GetMapHeight();
        void SendMap(NetConnection connection);
        int GetTileSpacing();

        ITile[] GetAllTilesIn(RectangleF Area);
        ITile[] GetAllFloorIn(RectangleF Area);
        ITile[] GetAllWallIn(RectangleF Area);

        ITile GetWallAt(Vector2 pos);
        ITile GetFloorAt(Vector2 pos);
        ITile[] GetAllTilesAt(Vector2 pos);

        ITile GenerateNewTile(Vector2 pos, string type, Direction dir = Direction.North);
        void DestroyTile(ITile s);
        RectangleF GetWorldArea();
    }
}