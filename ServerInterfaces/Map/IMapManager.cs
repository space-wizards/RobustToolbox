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
        void NetworkUpdateTile(Vector2 pos);
        void SaveMap();

        void MoveGasCell(ITile fromTile, ITile toTile);

        NetOutgoingMessage CreateMapMessage(MapMessage messageType);
        void SendMessage(NetOutgoingMessage message);
        void Shutdown();
        int GetMapWidth();
        int GetMapHeight();
        bool IsWorldPositionInBounds(Vector2 pos);
        bool IsSaneArrayPosition(int x, int y);
        void SendMap(NetConnection connection);
        int GetTileSpacing();

        ITile[] GetITilesIn(RectangleF Area);
        ITile GetITileAt(Vector2 WorldPos);
        ITile GenerateNewTile(Vector2 pos, string type);
        void DestroyTile(Vector2 pos);
        RectangleF GetWorldArea();
    }
}