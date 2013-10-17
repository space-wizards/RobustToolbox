using System;
using System.Drawing;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces.Tiles;

namespace ServerInterfaces.Map
{
    public interface IMapManager
    {
        bool InitMap(string mapName);
        void HandleNetworkMessage(NetIncomingMessage message);
        void NetworkUpdateTile(int x, int y);
        void SaveMap();
        ITile GetTileFromIndex(int x, int y);
        void DestroyTile(Point arrayPosition);
        void MoveGasCell(ITile fromTile, ITile toTile);
        bool ChangeTile(int x, int z, Type newType);
        ITile GenerateNewTile(int x, int y, string type);
        NetOutgoingMessage CreateMapMessage(MapMessage messageType);
        void SendMessage(NetOutgoingMessage message);
        void Shutdown();
        int GetMapWidth();
        int GetMapHeight();
        bool IsWorldPositionInBounds(Vector2 pos);
        Point GetTileArrayPositionFromWorldPosition(float x, float z);
        ITile GetTileFromWorldPosition(Vector2 pos);
        Point GetTileArrayPositionFromWorldPosition(Vector2 pos);
        Type GetTileTypeFromWorldPosition(float x, float z);
        bool IsSaneArrayPosition(int x, int y);
        void SendMap(NetConnection connection);
    }
}